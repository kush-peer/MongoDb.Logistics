using MongoDb.Logistics.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDb.Logistics.Constants;
using MongoDb.Logistics.Logging;

namespace MongoDb.Logistics.Database.Repositories
{
	/// <inheritdoc />
	public class PlanesRepo : IPlanesRepo
	{
		#region variables

		protected readonly IMongoClient client;
		protected readonly IMongoDatabase database;
		private readonly IMongoCollection<BsonDocument> collection;
		private readonly IAppLogger<PlanesRepo> logger;

		#endregion

		#region constructor

		/// <summary>
		/// Constructor for Planes Repository, Inject dependencies and initialize objects
		/// </summary>
		/// <param name="client"></param>
		/// <param name="logger"></param>
		public PlanesRepo(IMongoClient client, IAppLogger<PlanesRepo> logger)
		{
			this.client = client;
			this.database = client.GetDatabase(MongoDbConstant.DbName);
			var writeConcern = this.database.WithWriteConcern(WriteConcern.WMajority).WithReadConcern(ReadConcern.Majority);
			this.collection = writeConcern.GetCollection<BsonDocument>(MongoDbConstant.PlanesCollection);
			this.logger = logger;
		}

		#endregion

		#region Public Methods

		/// <inheritdoc />
		public async Task<IReadOnlyCollection<Plane>> GetPlanesAsync()
		{
			// Create thread safe plane collection ,keeps data in unordered manner 
			var planesCollection = new ConcurrentBag<Plane>();
			try
			{
				// Find all planes 
				var planeBsonAsyncCursor = await this.collection.FindAsync(new BsonDocument());

				// Convert mongo cursor to list
				var listOfPlanes = planeBsonAsyncCursor.ToList();

				// Runs upon multiple thread and processing takes place in parallel way
				Parallel.ForEach(listOfPlanes,
				  planeDoc =>
				  {
					  var plane = ReadPlane(planeDoc);
					  if (plane != null)
					  {
						  planesCollection.Add(plane);
					  }
				  });
			}
			catch (MongoException exception)
			{
				this.logger.LogError($"failed to read planes collection with exception as : {exception}");
			}

			// return list of collections in ascending order by call sign
			return planesCollection.OrderBy(x => x.Callsign).ToList().AsReadOnly();
		}

		/// <inheritdoc />
		public async Task<Plane> GetPlaneAsyncById(string callSign)
		{
			var plane = new Plane();
			try
			{
				// Create filter definition
				var filterById = new BsonDocument { [MongoDbConstant.Id] = callSign };

				// use find operation and return cursor
				var planeBsonAsyncCursor = await this.collection.FindAsync(filterById);

				// return into bson collection
				var plansCollection = planeBsonAsyncCursor.ToList();

				// Deserialize into plane collection object
				plane = ReadPlane(plansCollection.FirstOrDefault());
			}
			catch (MongoException exception)
			{
				this.logger.LogError($"failed to read planes collection with callsign id as :{callSign} exception as : {exception}");
			}

			return plane;
		}

		/// <inheritdoc />
		public async Task<bool> UpdatePlaneAsyncByLocationAndHeading(string callSign, List<double> location, float heading)
		{
			var result = false;
			try
			{
				// create filter definition by call sign
				var filterByDefinition = Builders<BsonDocument>.Filter.Eq(MongoDbConstant.Id,
				  callSign);

				// create update definition - set current location and heading 
				var updateFilter = Builders<BsonDocument>.Update
					.Set("currentLocation", location)
					.Set("heading", heading);

				// update plane collection
				var updatedPlane = await this.collection.UpdateOneAsync(filterByDefinition, updateFilter);

				// return acknowledgment from mongo  
				result = updatedPlane.IsAcknowledged;
			}
			catch (MongoException exception)
			{
				this.logger.LogError($"Failed to update the location and heading for plane for call sign : {callSign} with exception: {exception}");
			}

			return result;
		}

		/// <inheritdoc />
		public async Task<bool> UpdatePlaneAsyncByLocationHeadingAndLanded(string callSign, List<double> location, float heading, string landedCity)
		{
			var result = false;
			try
			{
				// Get the existing plane details 
				var latestPlane = await this.GetPlaneAsyncById(callSign);

				// Keep a copy of last landed city
				var lastLandedCity = latestPlane.Landed;

				if (!lastLandedCity.ToLower().Equals(landedCity.ToLower()))
				{
					// Keep a copy of last date time stamp
					var previousDateTimeStamp = latestPlane.CurrentDateTimeStamp;

					// define filter definition
					var filterDefinition = Builders<BsonDocument>.Filter.Eq(MongoDbConstant.Id, callSign);

					// set update definitions 
					var updateDefinition = Builders<BsonDocument>.Update
					  .Set("heading", heading)
					  .Set("landed", landedCity)
					  .Set("currentLocation", location)
					  .Set("lastLanded", lastLandedCity)
					  .Set("lastDateTimeStamp", previousDateTimeStamp)
					  .Set("currentDateTimeStamp", DateTime.UtcNow);

					// update plane collection
					var updatedPlane = await this.collection.UpdateOneAsync(filterDefinition, updateDefinition);

					// return success -1/ failure -0 
					result = updatedPlane.IsAcknowledged;
				}
			}
			catch (MongoException exception)
			{
				this.logger.LogError($"Failed to update the location, heading and landed city for plane for call sign : {callSign} with exception: {exception}");
			}

			return result;
		}

		/// <inheritdoc />
		public async Task<bool> ReplacePlaneAsyncByRoutes(string callSign, string city)
		{
			var result = false;
			try
			{
				// create filter definition - filter by call sign
				var filterDefinition = Builders<BsonDocument>.Filter.Eq(MongoDbConstant.Id,
				  callSign);

				// create filter definition - set routes by list of cities
				var updateDefinition = Builders<BsonDocument>.Update
					.Set("route", new List<string> {city});

				// update plane collection
				var updatedPlane = await this.collection.UpdateOneAsync(filterDefinition, updateDefinition);

				// return acknowledgment from mongo 
				result = updatedPlane.IsAcknowledged;
			}
			catch (MongoException exception)
			{
				this.logger.LogError($"Failed to replace route for plane with call sign : {callSign} with exception: {exception}");
			}

			return result;
		}

		/// <inheritdoc />
		public async Task<bool> AddPlaneAsyncByRoute(string callSign, string city)
		{
			var result = false;
			try
			{
				// filter definition - filter by call sign
				var filterDefinition = Builders<BsonDocument>.Filter.Eq(MongoDbConstant.Id,
				  callSign);

				// update definition - set route by city
				var updateDefinition = Builders<BsonDocument>.Update
					.AddToSet("route", city);

				// update plane collection
				var updatedPlane = await this.collection.UpdateOneAsync(filterDefinition, updateDefinition);

				// return acknowledgment from mongo  
				result = updatedPlane.IsAcknowledged;
			}
			catch (MongoException exception)
			{
				this.logger.LogError($"Failed to add plane for call sign : {callSign} with exception: {exception}");
			}

			return result;
		}

		/// <inheritdoc />
		public async Task<bool> DeleteAsyncByFirstPlaneRoute(string callSign)
		{
			var result = false;
			try
			{
				// filter definition -Filter by call sign
				var filterDefinition = Builders<BsonDocument>.Filter.Eq(MongoDbConstant.Id, callSign);

				// // update definition - delete route aka city 
				var updateDefinition = Builders<BsonDocument>.Update.PopFirst("route");

				// update plane collection
				var updatedPlane = await this.collection.UpdateOneAsync(filterDefinition, updateDefinition);

				// return acknowledgment from mongo 
				result = updatedPlane.IsAcknowledged;
			}
			catch (MongoException exception)
			{
				this.logger.LogError($"Failed to delete first route for plane with call sign : {callSign} with exception: {exception}");
			}

			return result;
		}

		/// <inheritdoc />
		public async Task<bool> UpdateMileageAndDuration(string id, City landedCity, City previousCity)
		{
			var result = false;
			const long maintenanceCheck = 50000;
			double flightDurationInSeconds = 0;
			try
			{
				// Get the existing plan details 
				var latestPlane = await this.GetPlaneAsyncById(id);

				// Calculate Distance from coordinates
				var distanceCoveredInMiles = await CalculateDistance(latestPlane.CurrentLocation, landedCity.Name);

				// Check if plan maintenance is required
				var IsMaintenanceRequired = latestPlane.TotalMileage + distanceCoveredInMiles > maintenanceCheck;

				// Check if last date stamp has value then calculate flight duration
				if (latestPlane.LastDateTimeStamp.HasValue)
				{
					flightDurationInSeconds = latestPlane.CurrentDateTimeStamp.Subtract(Convert.ToDateTime(latestPlane.LastDateTimeStamp)).TotalSeconds;
				}

				// define filter definition
				var filterDefinition = Builders<BsonDocument>.Filter.Eq(MongoDbConstant.Id, id);

				// set update definitions 
				var updateDefinition = Builders<BsonDocument>.Update
				  .Set("totalDuration", flightDurationInSeconds)
				  .Set("distanceCoveredInMiles", distanceCoveredInMiles)
				  .Set("isMaintenanceRequired", IsMaintenanceRequired);

				// Update plane collection
				var updatedPlaneResult = await this.collection.UpdateOneAsync(filterDefinition, updateDefinition);

				// return success -1/ failure -0 
				result = updatedPlaneResult.IsAcknowledged;
			}
			catch (MongoException exception)
			{
				this.logger.LogError($" Failed to update total distance covered by plane and total time for plane for cargo Id: {id} with exception as: {exception}");
			}
			return result;
		}


		#endregion

		#region private methods

		private static Plane ReadPlane(BsonDocument planeDoc)
		{
			var plane = BsonSerializer.Deserialize<Plane>(planeDoc);
			plane.Heading = Convert.ToDecimal($"{planeDoc.GetValue("heading").ToDecimal():N2}");
			return plane;
		}

		// Another way to calculate Distance from Coordinates
		//private static double CalculateDistance(IReadOnlyList<double> source, IReadOnlyList<double> destination)
		//{
		//	var sourceCoordinates = new GeoCoordinate(source[0], source[1]);
		//	var destinationCoordinates = new GeoCoordinate(destination[0], destination[1]);
		//	return sourceCoordinates.GetDistanceTo(destinationCoordinates);
		//}

		/// <summary>
		/// Calculate distance 
		/// </summary>
		/// <param name="sourceCoordinates"></param>
		/// <param name="destinationCity"></param>
		/// <returns>distance</returns>
		protected async Task<double> CalculateDistance(List<double> sourceCoordinates, string destinationCity)
		{
			// TODO we can create Aggregation definition and then use it - due to crunch of time, used this way
			var pipeline = new List<BsonDocument>()
			{
				new("$geoNear", new BsonDocument
				{
					{ "near",
						new BsonDocument
						{
							{ "type", "coordinates" },
							{ "coordinates",
								new BsonArray
								{
									sourceCoordinates[0],
									sourceCoordinates[1]
								}
							}
						}
					},
					{ "distanceField", "distance" },
					{ "query",
						new BsonDocument("_id", destinationCity)
					},
					{ "maxDistance", 100000000 },
					{ "minDistance", 1 },
					{ "spherical", false },
					{ "distanceMultiplier", 0.000621371 },
					{ "key", "position" }
				}),
				new("$limit", 1),
				new("$project", new BsonDocument("distance", 1))
			};

			var aggregationResult = await collection.AggregateAsync<BsonDocument>(pipeline);
			var result = aggregationResult.ToList();
			return result.Count > 0 ? result[0]["distance"].ToDouble() : 0;
		}

		#endregion
	}
}
