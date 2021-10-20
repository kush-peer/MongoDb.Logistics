using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDb.Logistics.ServiceRegistration.Interface;

namespace MongoDb.Logistics.ServiceRegistration
{
	public class CorsServiceRegistration : IServiceRegistration
	{
		/// <summary>
		/// Add cors policy
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configuration"></param>
		public void Configure(IServiceCollection services, IConfiguration configuration)
		{
			services.AddCors(c =>
			{
				c.AddPolicy("AllowOrigin", options => options.AllowAnyOrigin());
				{
					c.AddPolicy("AllowHeader", options => options.AllowAnyHeader());
					c.AddPolicy("AllowMethod", options => options.AllowAnyMethod());
				}
			});
		}
	}
}
