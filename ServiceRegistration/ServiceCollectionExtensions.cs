using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDb.Logistics.ServiceRegistration.Interface;

namespace MongoDb.Logistics.ServiceRegistration
{
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		///  Extension to initialize all service registration implementing IServiceRegistration
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="services"></param>
		/// <param name="configuration"></param>
		public static void RegisterAll<T>(this IServiceCollection services, IConfiguration configuration)
		{
			typeof(T)
				.Assembly.ExportedTypes
				.Where(x => typeof(IServiceRegistration).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
				.Select(Activator.CreateInstance)
				.Cast<IServiceRegistration>()
				.ToList()
				.ForEach(x => x.Configure(services, configuration));
		}
	}
}
