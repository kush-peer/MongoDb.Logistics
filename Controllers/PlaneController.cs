using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDb.Logistics.ActionFilters;
using MongoDb.Logistics.Database.Repositories;
using MongoDb.Logistics.Logging;
using MongoDb.Logistics.Models;

namespace MongoDb.Logistics.Controllers
{
	/// <inheritdoc />
	[Route("planes")]
	[ApiController]
	public class PlaneController : ControllerBase
	{
		private readonly IAppLogger<PlaneController> logger;
		private readonly IPlanesRepo PlanesRepo;
		private readonly ICitiesRepo CitiesRepo;
		
		public PlaneController(IPlanesRepo planesRepo, ICitiesRepo citiesRepo, IAppLogger<PlaneController> logger)
		{
			this.PlanesRepo = planesRepo;
			this.CitiesRepo = citiesRepo;
			this.logger = logger;
		}

		#region Public http API end point verbs

		/// <summary>
		/// Read the collection of planes
		/// </summary>
		/// <returns></returns>
		/// <response code="200">Query successful.</response>
		/// <response code="404">Data not found.</response>
		[HttpGet]
		[ProducesResponseType(typeof(IReadOnlyCollection<Plane>), 200)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult> GetPlanesAsync()
		{
			try
			{
				var planes = await this.PlanesRepo.GetPlanesAsync();
				return new OkObjectResult(planes);
			}
			catch (Exception exception)
			{
				this.logger.LogError(exception.Message);
				return new NotFoundResult();
			}
		}

		/// <summary>
		/// Read the plan by Id aka Call Sign
		/// </summary>
		/// <param name="callSign"> Request Body <br></br>
		/// <code> callSign : CARGO0 </code> <br></br></param>
		/// <returns></returns>
		/// <response code="200">Query successful.</response>
		/// <response code="404">Data not found.</response>
		[HttpGet("{callSign}")]
		[ProducesResponseType(typeof(Plane), 200)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult> GetPlanesAsyncById(string callSign)
		{
			try
			{
				var plane = await this.PlanesRepo.GetPlaneAsyncById(callSign);
				return new OkObjectResult(plane);
			}
			catch (Exception exception)
			{
				this.logger.LogError(exception.Message);
				return new NotFoundResult();
			}
		}

		/// <summary>
		/// Update Plane  By Location
		/// </summary>
		/// <param name="id">Request Body <br></br>
		/// <code> Id : CARGO0 </code> <br></br></param>
		/// <param name="location">Request Body <br></br>
		/// <code> location :55.3,-2.5  </code> <br></br></param>
		/// <param name="heading">Request Body <br></br>
		/// <code> heading : 10 </code> <br></br></param>
		/// <returns></returns>
		/// <response code="204">Data updated.</response>
		/// <response code="404">Data not found.</response>

		[HttpPut("{id}/location/{location?}/{heading}")]
		[ArrayInput("location")]
		[ProducesResponseType(typeof(Plane), 204)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> UpdatePlaneAsyncByLocationAndHeading(string id, [FromRoute] string[] location, int heading)
		{
			try
			{
				if (!Enumerable.Range(0, 360).Contains(heading))
				{
					return new BadRequestObjectResult("Header is not valid and is out of Range");
				}

				if (location.ToList().Count != 2)
				{
					return new BadRequestObjectResult("Location information is invalid");
				}

				var result = await this.PlanesRepo.UpdatePlaneAsyncByLocationAndHeading(id, location.Select(double.Parse).ToList(), heading);

				return new OkObjectResult(result);
			}
			catch (Exception exception)
			{
				this.logger.LogError(exception.Message);
				return new BadRequestObjectResult("Object not found");
			}
		}

		/// <summary>
		/// Update Plane  By Location Heading And LandedCity
		/// </summary>
		/// <param name="id">Request Body <br></br>
		/// <code> Id : CARGO0 </code> <br></br></param>
		/// <param name="location">Request Body <br></br>
		/// <code> location : 55.3,-2.5 </code> <br></br></param>
		/// <param name="heading">Request Body <br></br>
		/// <code> heading : 10 </code> <br></br></param>
		/// <param name="city">Request Body <br></br>
		/// <code> heading : Mumbai </code> <br></br></param>
		/// <returns></returns>
		/// <response code="202">Data Updated.</response>
		/// <response code="404">Data not found.</response>
		[HttpPut("{id}/location/{location?}/{heading}/{city}")]
		[ArrayInput("location")]
		[ProducesResponseType(typeof(Plane), 202)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> UpdatePlaneAsyncByLocationHeadingAndLandedCity(string id, [FromRoute] string[] location, int heading, string city)
		{
			try
			{
				if (!Enumerable.Range(0, 360).Contains(heading))
				{
					return new BadRequestObjectResult("Header is not valid and is out of Range");
				}

				if (location.ToList().Count != 2)
				{
					return new BadRequestObjectResult("Location information is invalid");
				}

				var cityCol = await this.CitiesRepo.GetCityAsyncById(city);

				if (cityCol == null)
				{
					return new BadRequestObjectResult("City not Found");
				}

				var result = await this.PlanesRepo.UpdatePlaneAsyncByLocationHeadingAndLanded(id, location.Select(double.Parse).ToList(), heading, city);

				return new OkObjectResult(result);
			}
			catch (Exception exception)
			{
				this.logger.LogError(exception.Message);
				return new BadRequestObjectResult("Object Not found");
			}
		}

		/// <summary>
		/// Replace Plane ByRoutes
		/// </summary>
		/// <param name="id">Request Body <br></br>
		/// <code> Id : CARGO0 </code> <br></br></param>
		/// <param name="city">Request Body <br></br>
		/// <code> heading : Mumbai </code> <br></br></param>
		/// <returns></returns>
		/// <response code="204">Data updated successful.</response>
		/// <response code="404">Data not found.</response>
		[HttpPut("{id}/route/{city}")]
		[ProducesResponseType(typeof(Plane), 204)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> ReplacePlaneAsyncByRoutes(string id, string city)
		{
			try
			{
				var cityCol = await this.CitiesRepo.GetCityAsyncById(city);
				if (cityCol == null)
				{
					return new BadRequestObjectResult("City not Found");
				}
				var result = await this.PlanesRepo.ReplacePlaneAsyncByRoutes(id, city);
				return new OkObjectResult(result);
			}
			catch (Exception exception)
			{
				this.logger.LogError(exception.Message);
				return new BadRequestObjectResult("Object Not Found");
			}
		}

		/// <summary>
		/// Add Plane Async By Route
		/// </summary>
		/// <param name="id"></param>
		/// <param name="city"></param>
		/// <returns></returns>
		/// <response code="201">Data updated successful.</response>
		/// <response code="404">Data not found.</response>
		[HttpPost("{id}/route/{city}")]
		[ProducesResponseType(typeof(Plane), 201)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> AddPlaneAsyncByRoute(string id, string city)
		{
			try
			{
				var cityCol = await this.CitiesRepo.GetCityAsyncById(city);
				if (cityCol == null)
				{
					return new BadRequestObjectResult("City not Found");
				}
				var result = await this.PlanesRepo.AddPlaneAsyncByRoute(id, city);
				return new OkObjectResult(result);
			}
			catch (Exception exception)
			{
				this.logger.LogError(exception.Message);
				return new BadRequestObjectResult("Object Not Found");
			}
		}

		/// <summary>
		/// Delete City 
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		/// <response code="204">Data deleted successful.</response>
		/// <response code="404">Data not found.</response>
		[HttpDelete("{id}/route/destination")]
		[ProducesResponseType(typeof(Plane), 204)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> RemoveFirstPlaneRoute(string id)
		{
			try
			{
				var result = await this.PlanesRepo.DeleteAsyncByFirstPlaneRoute(id);
				if (!result)
				{
					return new BadRequestObjectResult($"Failed to remove city for id {id}");
				}

				return new JsonResult(result);
			}
			catch (Exception exception)
			{
				this.logger.LogError(exception.Message);
				return new BadRequestObjectResult("Object Not Found");
			}

		}

		#endregion
	}
}
