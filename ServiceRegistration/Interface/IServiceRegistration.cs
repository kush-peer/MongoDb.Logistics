using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MongoDb.Logistics.ServiceRegistration.Interface
{
	public interface IServiceRegistration
	{
		void Configure(IServiceCollection services, IConfiguration configuration);
	}
}
