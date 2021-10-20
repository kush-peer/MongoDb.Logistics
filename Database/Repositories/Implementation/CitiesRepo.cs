using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDb.Logistics.Constants;
using MongoDb.Logistics.Logging;
using MongoDb.Logistics.Models;

namespace MongoDb.Logistics.Database.Repositories.Implementation
{
	/// <inheritdoc />
	public class CitiesRepo : ICitiesRepo
	{
		#region variables

		protected readonly IMongoClient client;
		protected readonly IMongoDatabase database;
		private readonly IMongoCollection<BsonDocument> collection;
		private readonly IAppLogger<CitiesRepo> logger;

		#endregion

		#region constructor
		/// <summary>
		/// Constructor for Cities Repository, Inject dependencies and initialize objects
		/// </summary>
		/// <param name="client"></param>
		/// <param name="logger"></param>
		public CitiesRepo(IMongoClient client, IAppLogger<CitiesRepo> logger)
		{
			this.client = client;
			this.database = client.GetDatabase(MongoDbConstant.DbName);
			var writeConcern = this.database.WithWriteConcern(WriteConcern.WMajority).WithReadConcern(ReadConcern.Majority);
			this.collection = writeConcern.GetCollection<BsonDocument>(MongoDbConstant.CitiesCollection);
			this.logger = logger;
		}
		#endregion

		#region public methods

		/// <inheritdoc />
		public async Task<IReadOnlyCollection<City>> GetCitiesAsync()
		{
			try
			{
				// Find cities collection
				var citiesAsyncCursor = await this.collection.FindAsync(new BsonDocument());
				var citiesCollection = citiesAsyncCursor.ToList();

				// Create thread safe collection ,keeps data in unordered manner  
				var cities = new ConcurrentBag<City>();

				// Runs upon multiple thread and processing takes place in parallel way
				Parallel.ForEach(citiesCollection, cityDoc =>
				{
					var cityModel = BsonSerializer.Deserialize<City>(cityDoc);
					cities.Add(cityModel);
				});

				//  Order by city name ascending 
				return cities.OrderBy(x => x.Name).ToList().AsReadOnly();
			}
			catch (MongoException mongoException)
			{
				this.logger.LogError($"MongoDb client failed to read cities from collection with exception as: {mongoException}");
			}
			return null;
		}

		/// <inheritdoc />
		public async Task<City> GetCityAsyncById(string cityId)
		{
			try
			{
				// Get City info 
				var city = await GetCityById(cityId);
				return city;
			}
			catch (MongoException mongoException)
			{
				this.logger.LogError($"MongoDb client failed to read cities from collection by Id: {cityId} with exception as: {mongoException}");
			}
			return null;
		}

		/// <inheritdoc />
		public async Task<NeighborCities> GetNearestCitiesAsync(string cityId, int count)
		{
			var nearestCities = new NeighborCities();
			try
			{
				// Get City by Id
				var city = await this.GetCityById(cityId);

				// Create filter  definition
				var filterPoint = GeoJson.Point(new GeoJson2DCoordinates(city.Location[0], city.Location[1]));
				var filterDefinition = new FilterDefinitionBuilder<BsonDocument>()
					.NearSphere("position", filterPoint, minDistance: 1);

				// use find operation and 
				var listOfCities = await collection.Find(filterDefinition).Limit(count).ToListAsync();
				
				var result = new List<City>();

				// Deserialize and add into city  collection
				result.AddRange(listOfCities.Select(cityDoc => BsonSerializer.Deserialize<City>(cityDoc)));

				// return into Neighbor collection 
				// TODO - since NeighborCities is almost duplicate of city collection, we can use dynamic json creation method 
				return new NeighborCities
				{
					Neighbors = result
				};
			}
			catch (MongoException mongoException)
			{
				this.logger.LogError($"MongoDb client failed to read cities from collection by Id: {cityId} with exception as: {mongoException}");
			}

			return nearestCities;
		}

		#endregion

		#region protected Methods

		/// <summary>
		/// Get City by city id
		/// </summary>
		/// <param name="Id"></param>
		/// <returns></returns>
		protected async Task<City> GetCityById(string Id)
		{
			// define filter by using builder helper
			var filterById = new BsonDocument { [MongoDbConstant.Id] = Id };
			var cursor = await this.collection.FindAsync(filterById);
			var listOfCities = cursor.ToList();
			var city = BsonSerializer.Deserialize<City>(listOfCities.FirstOrDefault());
			return city;
		}

		#endregion
	}
}
