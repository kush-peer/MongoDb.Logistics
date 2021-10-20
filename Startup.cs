using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MongoDb.Logistics.Database.ChangeStream;
using MongoDb.Logistics.ServiceRegistration;

namespace MongoDb.Logistics
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
		Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{

		services.AddControllers();
		services.RegisterAll<Startup>(this.Configuration);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env, CargoChangeStreamService cargoChangeStreamService)
		{
		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
			app.UseSwagger();
			app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MongoDb.Logistics v1"));
		}

		// Load static index.html file http://localhost:5000/static/index.html
		app.UseFileServer(new FileServerOptions()
		{
			EnableDefaultFiles = true,
			FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "static")),
			RequestPath = "/static"
		});

		// Register change stream service 
		new Thread(cargoChangeStreamService.Init).Start();

		app.UseStaticFiles();

		app.UseHttpsRedirection();

		app.UseRouting();

		app.UseAuthorization();

		app.UseEndpoints(endpoints =>
		{
		endpoints.MapControllers();
		});
		}
	}
}
