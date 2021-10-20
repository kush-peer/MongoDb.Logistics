using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using MongoDb.Logistics.ServiceRegistration.Interface;

namespace MongoDb.Logistics.ServiceRegistration
{

	/// <summary>
	/// Add Swagger documentation
	/// </summary>
	public class SwaggerServiceRegistration : IServiceRegistration
	{
		/// <summary>
		/// Add swagger documentation
		/// </summary>
		/// <param name="services">Inject services interface from startup</param>
		/// <param name="configuration">Inject configuration interface from startup</param>
		public void Configure(IServiceCollection services, IConfiguration configuration)
		{
			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo
				{
					Title = "MongoDb.Logistics",
					Version = "v1.0",
					Description = "List of Logistics Api End points, these Apis are orchestration for different logistic operations"
				});
				var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
				var commentsFileName = Assembly.GetExecutingAssembly().GetName().Name + ".XML";
				var commentsFile = Path.Combine(
					baseDirectory,
					commentsFileName);
				c.IncludeXmlComments(commentsFile);
			});
		}
	}
}
