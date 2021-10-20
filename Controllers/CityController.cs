using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDb.Logistics.Database.Repositories;
using MongoDb.Logistics.Logging;
using MongoDb.Logistics.Models;

namespace MongoDb.Logistics.Controllers
{
	/// <inheritdoc />
	[Route("cities")]
	[ApiController]
	public class CityController : ControllerBase
	{
		private readonly IAppLogger<CityController> logger;
		private readonly ICitiesRepo citiesRepo;

		public CityController(IAppLogger<CityController> logger, ICitiesRepo citiesRepo)
		{
			this.logger = logger;
			this.citiesRepo = citiesRepo;
		}

		#region Public http API end point verbs

		/// <summary>
		/// Read the collection of cities
		/// </summary>
		/// <returns></returns>
		/// <response code="200">Query successful.</response>
		/// <response code="404">Data not found.</response>
		[HttpGet]
		[ProducesResponseType(typeof(IReadOnlyCollection<City>), 200)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult> GetCitiesAsync()
		{
			try
			{
				var cities = await this.citiesRepo.GetCitiesAsync();
				return new JsonResult(cities);
			}
			catch (Exception exception)
			{
				this.logger.LogError(exception.Message);
				return new NotFoundResult();
			}
		}

		/// <summary>
		/// Get City Info detail
		/// </summary>
		/// <param name="id"> Request Body <br></br>
		/// <code> cityId : Mumbai </code> <br></br></param>
		/// <returns>Returns City Details</returns>
		/// <response code="200">Query successful.</response>
		/// <response code="404">Data not found.</response>
		[HttpGet("{id}")]
		[ProducesResponseType(typeof(City), 200)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult> GetCityAsyncById(string id)
		{
			try
			{
				var city = await this.citiesRepo.GetCityAsyncById(id);
				return new JsonResult(city);
			}
			catch (ArgumentException exception)
			{
				this.logger.LogError(exception.Message);
				return new NotFoundResult();
			}
		}

		/// <summary>
		/// Get City Info detail
		/// </summary>
		/// <param name="id"> Request Body <br></br>
		/// <code> cityId : Mumbai </code> <br></br></param>
		/// <param name="count">Request Body <br></br>
		/// <code> count : 10 </code> <br></br></param>
		/// <returns>Get Nearest City ById</returns>
		/// <response code="404">Data not found.</response>
		[HttpGet("{id}/neighbors/{count:int}")]
		[ProducesResponseType(typeof(NeighborCities), 200)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult> GetNearestCityAsyncById(string id, int count)
		{
			try
			{
				var neighbors = await this.citiesRepo.GetNearestCitiesAsync(id, count);

				return new JsonResult(neighbors);
			}
			catch (ArgumentException exception)
			{
				this.logger.LogError(exception.Message);
				return new NotFoundResult();
			}
		}

		#endregion
	}
}
