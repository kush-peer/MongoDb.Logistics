using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDb.Logistics.Constants;
using MongoDb.Logistics.Database.Repositories;
using MongoDb.Logistics.Logging;
using MongoDb.Logistics.Models;

namespace MongoDb.Logistics.Database.ChangeStream
{
	public class CargoChangeStreamService
	{
		protected readonly IMongoClient client;
		protected readonly IMongoDatabase database;
		private readonly IMongoCollection<BsonDocument> collection;
		private readonly ICitiesRepo citiesRepo;
		private readonly IPlanesRepo planesRepo;
		private readonly IAppLogger<CargoChangeStreamService> logger;
		private readonly SemaphoreSlim semaphoreSlim = new(1, 1);

		public CargoChangeStreamService(IMongoClient client, ICitiesRepo citiesRepo, IPlanesRepo planesRepo,
			IAppLogger<CargoChangeStreamService> logger)
		{
			this.database = client.GetDatabase(MongoDbConstant.DbName);
			this.collection = this.database.GetCollection<BsonDocument>(MongoDbConstant.PlanesCollection);
			this.client = client;
			this.citiesRepo = citiesRepo;
			this.planesRepo = planesRepo;
			this.logger = logger;
		}

		/// <summary>
		/// Initialize Thread for Mongodb Change stream service
		/// </summary>
		public void Init()
		{
			new Thread(async () => await ObservePlanLandings()).Start();
		}

		#region private methods

		/// <summary>
		/// Change stream to watch plan landings
		/// </summary>
		private async Task ObservePlanLandings()
		{
			// Filter definition for document updated 
			var pipelineFilterDefinition = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
				.Match(x => x.OperationType == ChangeStreamOperationType.Update);

			// choose stream option and set data lookup for full document 
			var changeStreamOptions = new ChangeStreamOptions
			{
				FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
			};

			// Watches changes on the collection , no need to user cancellation token, mongo sdk already using it
			using var cursor = await this.collection.WatchAsync(pipelineFilterDefinition, changeStreamOptions);

			await this.semaphoreSlim.WaitAsync();

			// Run watch updated operations on returned cursor  from watch async
			await this.WatchPlaneUpdates(cursor);

			// release thread
			this.semaphoreSlim.Release();
		}

		/// <summary>
		/// Track Plan changes by landed property and calculate Mileage and Duration 
		/// </summary>
		/// <param name="cursor"></param>
		/// <returns></returns>

		private async Task WatchPlaneUpdates(IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> cursor)
		{
			await cursor?.ForEachAsync(async change =>
			{
				// If change is null - logger information as null and return 
				if (change == null)
				{
					this.logger.LogInformation("No changes tracked for plane by change stream plane watcher");
					return;
				}
				try
				{
					// Check if updated fields contain landed property
					var result = change.UpdateDescription.UpdatedFields.Contains("landed");
					if (!result)
					{
						// if false return 
						return;
					}

					// Deserialize full document with Plan object 
					// TODO explore an option get selected object from change rather than full document
					var landedPlaneInfo = BsonSerializer.Deserialize<Plane>(change.FullDocument);

					// if current landed property is not equal to last 
					if (!landedPlaneInfo.Landed.Equals(landedPlaneInfo.LastLanded))
					{
						this.logger.LogInformation($"Change stream plane watcher started for call sign : {landedPlaneInfo.Callsign}");
						// Get latest copy of City collection 
						var landedCity = await this.citiesRepo.GetCityAsyncById(landedPlaneInfo.Landed);

						// Get last copy of city collection
						var previousCity = await this.citiesRepo.GetCityAsyncById(landedPlaneInfo.LastLanded);

						// Update Mileage , Duration and Status
						await this.planesRepo.UpdateMileageAndDuration(landedPlaneInfo.Callsign, landedCity, previousCity);

						// log information
						this.logger.LogInformation($"Changes tracked successfully for plane by change stream plane watcher for call sign : {landedPlaneInfo.Callsign}");
					}
				}
				catch (MongoException exception)
				{
					// log mongo exception - helpful for developers
					this.logger.LogError("Change Stream Plane watcher. Exception:" + exception);
				}
			});
		}


		#endregion

	}
}
