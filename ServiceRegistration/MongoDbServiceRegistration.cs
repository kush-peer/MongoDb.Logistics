using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDb.Logistics.ServiceRegistration.Interface;
using MongoDB.Driver;
using MongoDb.Logistics.Database.ChangeStream;
using MongoDb.Logistics.Database.Repositories;
using MongoDb.Logistics.Database.Repositories.Implementation;
using MongoDb.Logistics.Database.Repositories.Interfaces;
using MongoDb.Logistics.Logging;

namespace MongoDb.Logistics.ServiceRegistration
{
	/// <inheritdoc />
	public class MongoDbServiceRegistration : IServiceRegistration
	{
		/// <inheritdoc />
		public void Configure(IServiceCollection services, IConfiguration configuration)
		{
			#region MongoDB Injection
			services.AddSingleton<IMongoClient>(x => new MongoClient(configuration["mongoDb-connection"]));
			#endregion

			#region Mongo Repos Injection
			services.AddSingleton<ICitiesRepo, CitiesRepo>();
			services.AddSingleton<IPlanesRepo, PlanesRepo>();
			services.AddSingleton<ICargoRepo, CargoRepo>();
			#endregion

			#region MongoDB Change Stream Injection
			services.AddSingleton<CargoChangeStreamService, CargoChangeStreamService>();
			#endregion

			#region logging Service Injection
			services.AddSingleton(typeof(IAppLogger<>), typeof(LoggerAdapter<>));

			#endregion
		}
	}
}
