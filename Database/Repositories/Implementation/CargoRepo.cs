using MongoDb.Logistics.Database.Repositories.Interfaces;
using MongoDb.Logistics.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDb.Logistics.Logging;
using MongoDb.Logistics.Constants;
using System.Collections.Concurrent;
using System.Linq;
using MongoDB.Bson.Serialization;

namespace MongoDb.Logistics.Database.Repositories.Implementation
{
	public class CargoRepo : ICargoRepo
	{
		#region variables

		protected readonly IMongoClient client;
		protected readonly IMongoDatabase database;
		private readonly IMongoCollection<BsonDocument> collection;
		private readonly IAppLogger<CargoRepo> logger;

		#endregion

		/// <summary>
		/// Constructor for cargo Repository, Inject dependencies and initialize objects
		/// </summary>
		public CargoRepo(IMongoClient client, IAppLogger<CargoRepo> logger)
		{
			this.client = client;
			this.database = client.GetDatabase(MongoDbConstant.DbName);
			var writeConcern = this.database.WithWriteConcern(WriteConcern.WMajority).WithReadConcern(ReadConcern.Majority);
			this.collection = writeConcern.GetCollection<BsonDocument>(MongoDbConstant.CargoCollection);
			this.logger = logger;
		}

		/// <inheritdoc />
		public async Task<(bool, Cargo)> CreateNewCargo(string location, string destination)
		{
			var cargoUpdated = new Cargo();
			var isUpdated = false;
			try
			{
				// Create object id
				var id = new ObjectId();

				// Create Cargo Model needs te be updated
				var cargo = new BsonDocument()
				{
					{MongoDbConstant.Id, id},
					{"location",  location},
					{"destination",  destination},
					{"courierSource",  location},
					{"courierDestination",  destination},
					{"status", MongoDbConstant.InProgress},
					{"received", new BsonDateTime(DateTime.Now).ToUniversalTime()}
				};

				// Add cargo model
				await this.collection.InsertOneAsync(cargo);

				// Get the copy of updated model
				cargoUpdated = await this.GetCargoByCargoId(cargo[MongoDbConstant.Id].ToString());

				// return success as acknowledgement 
				isUpdated = true;
			}
			catch (MongoException mongoException)
			{
				this.logger.LogError($"MongoDb client failed to add cargo with exception as: {mongoException}");
			}

			// return status and cargo object
			return (isUpdated, cargoUpdated);
		}

		/// <inheritdoc />
		public async Task<(bool, long)> UpdateCargoStatusAndDuration(Cargo cargo, string status)
		{
			// define local variables
			var isUpdated = false;
			long result = 0;
			var cargoId = cargo.Id;
			var currentDateTime = new BsonDateTime(DateTime.Now).ToUniversalTime();
			var duration = currentDateTime - cargo.Received;
			try
			{

				// create filter definition
				var filterDefinition = FilterDefinitionByCargoId(cargoId);

				// create update definition 
				var updateDefinition = Builders<BsonDocument>.Update
					.Set("status", status)
					.Set("deliveredAt", currentDateTime)
					.Set("duration", duration.TotalMilliseconds);

				// update cargo collection
				var updatedCargoResult = await this.collection.UpdateOneAsync(filterDefinition, updateDefinition);

				// return success -1/ failure -0 and modified count as well
				// Note: will update planes repo as well - better to have acknowledgment as well as modified count from mongo response 
				isUpdated = updatedCargoResult.IsAcknowledged;
				result = updatedCargoResult.ModifiedCount;

			}
			catch (MongoException exception)
			{
				this.logger.LogError($"Failed to update the cargo for cargoId: {cargo.Id} having status: {status} with exception: {exception}");
			}
			return (isUpdated, result);
		}

		/// <inheritdoc />
		public async Task<(bool, long)> UpdateCargoCourierByCargoId(string cargoId, string courier)
		{
			long result = 0;
			var isUpdated = false;
			try
			{
				// create filter definition 
				var filterDefinition = FilterDefinitionByCargoId(cargoId);

				// create update definition - set courier
				var updateDefinition = Builders<BsonDocument>.Update.Set("courier", courier);

				// Update Cargo collection
				var updatedCargoResult = await this.collection.UpdateOneAsync(filterDefinition, updateDefinition);

				// record status and modified count
				isUpdated = updatedCargoResult.IsAcknowledged;
				result = updatedCargoResult.ModifiedCount;
			}
			catch (MongoException exception)
			{
				this.logger.LogError($"Failed to updated courier : {courier} to the cargo : {cargoId} with exception as : {exception}");
			}
			return (isUpdated, result);
		}

		/// <inheritdoc />
		public async Task<(bool, long)> DeleteCourierByCargoId(string cargoId)
		{
			long result = 0;
			var isUpdated = false;
			try
			{
				// Create Filter definition
				var filterByCargoId = Builders<BsonDocument>.Filter.Eq(MongoDbConstant.Id, new ObjectId(cargoId));

				// Create Update  Definition
				var updateDefinition = Builders<BsonDocument>.Update.Unset("courier");

				// update Cargo Collection 
				var updatedCargoResult = await this.collection.UpdateOneAsync(filterByCargoId, updateDefinition);

				// record status and modified count
				result = updatedCargoResult.ModifiedCount;
				isUpdated = updatedCargoResult.IsAcknowledged;
			}
			catch (MongoException exception)
			{
				this.logger.LogError($"Failed to remove the courier from the cargo : {cargoId}.Exception: {exception}");
			}
			return (isUpdated, result);
		}

		/// <inheritdoc />
		public async Task<(bool, long)> UpdateCargoSourceByLocation(string cargoId, string location)
		{
			long result = 0;
			var isUpdated = false;
			try
			{
				// create filter definition
				var filterDefinition = FilterDefinitionByCargoId(cargoId);

				// create update definition 
				var updateDefinition = Builders<BsonDocument>.Update
					.Set("location", location);

				// update cargo collection 
				var updatedCargoResult = await this.collection.UpdateOneAsync(filterDefinition, updateDefinition);

				// record status and modified count
				isUpdated = updatedCargoResult.IsAcknowledged;
				result = updatedCargoResult.ModifiedCount;

			}
			catch (MongoException exception)
			{
				this.logger.LogError($"Failed to update the source location : {location} for the cargo : {cargoId} with exception: {exception}");
			}
			return (isUpdated, result);
		}

		/// <inheritdoc />
		public async Task<(bool, IReadOnlyList<Cargo>)> GetAllCargosByLocation(string location)
		{
			// Create thread safe cargo collection ,keeps data in unordered manner 
			var cargos = new ConcurrentBag<Cargo>();
			var isUpdated = false;
			try
			{
				// create filter definitions 
				var filtersDefinitions = Builders<BsonDocument>.Filter.Eq("status", "in process")
										 & Builders<BsonDocument>.Filter.Eq("location", location);

				// use find command
				var cursor = await this.collection.Find(filtersDefinitions).ToListAsync();

				// Runs upon multiple thread and processing takes place in parallel way
				Parallel.ForEach(cursor, cargoDoc =>
				{
					var cargoModel = BsonSerializer.Deserialize<Cargo>(cargoDoc);
					cargos.Add(cargoModel);
				});
				isUpdated = true;
			}

			catch (MongoException exception)
			{
				this.logger.LogError($"Failed to Read the cargoes by location: {location}.Exception: {exception}");
			}
			// Sort cargos by ascending order
			return (isUpdated, cargos.OrderBy(x => x.Id).ToList().AsReadOnly());
		}

		/// <inheritdoc />
		public async Task<(bool, Cargo)> GetCargoById(string id)
		{
			var updatedCargo = new Cargo();
			var isUpdated = false;
			try
			{
				updatedCargo = await GetCargoByCargoId(id);
				isUpdated = true;
			}

			catch (MongoException exception)
			{
				this.logger.LogError($"Failed to Read the cargoes by cargo Id: {id} with exception: {exception}");
			}
			return (isUpdated, updatedCargo);
		}

		/// <inheritdoc />
		public async Task<(bool, double)> GetAverageDeliveryTime()
		{
			var averageDeliveryTime = 0.0;
			var isUpdated = false;
			try
			{
				// create filter definition
				var filterDefinition = Builders<BsonDocument>.Filter.Exists("duration");

				// create Projection
				var projection = Builders<BsonDocument>.Projection.Include("duration").Exclude(MongoDbConstant.Id);

				// filter results and project duration only
				var cargoList = await this.collection.Find(filterDefinition)
					.Project(projection)
					.ToListAsync();

				isUpdated = true;

				// convert duration to double 
				var durations = cargoList.ToList().Select(x => x.GetValue("duration").AsDouble);

				// calculate average time
				if (durations.Any())
				{
					averageDeliveryTime = durations.Sum() / durations.Count();
				}

			}
			catch (MongoException exception)
			{
				this.logger.LogError($"Failed to calculate average duration / delivery time  with exception: {exception}");
			}

			return (isUpdated, averageDeliveryTime);
		}

		#region private methods

		/// <summary>
		/// Get cargo by Id
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		private async Task<Cargo> GetCargoByCargoId(string id)
		{
			// create filter definition 
			var filterDefinition = new BsonDocument { [MongoDbConstant.Id] = new ObjectId(id) };

			// use find cursor to get cargos
			var cursor = await this.collection.FindAsync(filterDefinition);
			var listOfCargos = cursor.ToList();

			// deserialize into Cargo collection
			var updatedCargo = BsonSerializer.Deserialize<Cargo>(listOfCargos.FirstOrDefault());
			return updatedCargo;
		}

		/// <summary>
		/// create filter definition
		/// </summary>
		/// <param name="cargoId"></param>
		/// <returns></returns>

		private static BsonDocument FilterDefinitionByCargoId(string cargoId)
		{
			// create filter definition 
			var filterDefinition = new BsonDocument { [MongoDbConstant.Id] = new ObjectId(cargoId) };
			return filterDefinition;
		}
		#endregion

	}
}
