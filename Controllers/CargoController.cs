using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDb.Logistics.Database.Repositories;
using MongoDb.Logistics.Database.Repositories.Interfaces;
using MongoDb.Logistics.Logging;
using MongoDb.Logistics.Models;

namespace MongoDb.Logistics.Controllers
{
	/// <inheritdoc />
	[Route("cargo")]
	[ApiController]
	public class CargoController : ControllerBase
	{
		private readonly IAppLogger<CargoController> logger;
		private readonly ICargoRepo cargoRepo;
		private readonly ICitiesRepo citiesRepo;
		private readonly IPlanesRepo planesRepo;

		public CargoController(IAppLogger<CargoController> logger, ICargoRepo cargoRepo, ICitiesRepo citiesRepo, IPlanesRepo planesRepo)
		{
			this.logger = logger;
			this.cargoRepo = cargoRepo;
			this.citiesRepo = citiesRepo;
			this.planesRepo = planesRepo;
		}

		#region Public http API end point verbs

		/// <summary>
		/// Create a new cargo at "location" which needs to get to "destination"
		/// - error if neither location nor destination exist as cities.
		/// Set status to "in progress" 
		/// </summary>
		/// <param name="location">Request Body <br></br>
		/// <code> location : Mumbai </code> <br></br></param>
		/// <param name="destination">Request Body <br></br>
		/// <code> destination : Mexico </code> <br></br></param>
		/// <returns></returns>
		/// <response code="201">Data updated successful.</response>
		/// <response code="404">Data not found.</response>
		[HttpPost("{location}/to/{destination}")]
		[ProducesResponseType(typeof(Plane), 201)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> CreateCargoFromLocationToDestination(string location, string destination)
		{
			try
			{
				if (string.IsNullOrEmpty(location) || string.IsNullOrEmpty(destination))
				{
					return new BadRequestObjectResult("Either location nor Destination is invalid");
				}

				var sourceLocation = await this.citiesRepo.GetCityAsyncById(location);

				if (sourceLocation == null)
				{
					return new NotFoundObjectResult("Location city not found");
				}

				var destinationCity = await this.citiesRepo.GetCityAsyncById(destination);
				if (destinationCity == null)
				{
					return new NotFoundObjectResult("Destination city not found");
				}

				var newCargo = await this.cargoRepo.CreateNewCargo(location, destination);
				return new OkObjectResult(newCargo.Item2);
			}
			catch (ArgumentException exception)
			{
				this.logger.LogError(exception.Message);
				return new NotFoundResult();
			}

		}

		/// <summary>
		/// Set status field to 'Delivered' and delivered date
		/// </summary>
		/// <param name="id">Request Body <br></br>
		/// <code> id : 616e3c0d7752e55cd219bf99 </code> <br></br></param>
		/// <returns></returns>
		/// <response code="204">Data updated successful.</response>
		/// <response code="404">Data not found.</response>
		[HttpPut("{id}/delivered")]
		[ProducesResponseType(typeof(Plane), 204)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> CargoDelivered(string id)
		{
			try
			{
				(bool, long) result = default;
				var cargo = await this.cargoRepo.GetCargoById(id);

				if (cargo.Item2.CourierDestination.Equals(cargo.Item2.Destination))
				{
					result = await this.cargoRepo.UpdateCargoStatusAndDuration(cargo.Item2, "Delivered");
					if (!result.Item1)
					{
						return new BadRequestObjectResult("Invalid CargoId");
					}
				}
				return new OkObjectResult(result.Item1);
			}
			catch (ArgumentException exception)
			{
				this.logger.LogError(exception.Message);
				return new NotFoundResult();
			}

		}

		/// <summary>
		/// Marks that the next time the courier (plane) arrives at the location of this package it should be on
		/// loaded by setting the courier field - courier should be a plane.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="courier"></param>
		/// <returns></returns>
		/// <response code="204">Data updated successful.</response>
		/// <response code="404">Data not found.</response>
		[HttpPut("{id}/courier/{courier}")]
		[ProducesResponseType(typeof(Plane), 204)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> CargoAssignCourierById(string id, string courier)
		{
			try
			{
				(bool, long) result = default;
				result = await this.cargoRepo.UpdateCargoCourierByCargoId(id, courier);

				if (!result.Item1)
				{
					return new BadRequestObjectResult("Invalid CargoId");
				}

				return new JsonResult(true);
			}

			catch (ArgumentException exception)
			{
				this.logger.LogError(exception.Message);
				return new NotFoundResult();
			}

		}

		/// <summary>
		/// Set the value of courier on a given piece of cargo to null
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		/// <response code="204">Data deleted successful.</response>
		/// <response code="404">Data not found.</response>
		[HttpDelete("{id}/courier")]
		[ProducesResponseType(typeof(Plane), 204)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> DeleteCargoCourier(string id)
		{
			try
			{
				var result = await this.cargoRepo.DeleteCourierByCargoId(id);
				if (!result.Item1)
				{
					return new BadRequestObjectResult("Invalid CargoId");
				}

				return new OkObjectResult(result.Item1);
			}

			catch (ArgumentException exception)
			{
				this.logger.LogError(exception.Message);
				return new NotFoundResult();
			}

		}

		/// <summary>
		/// Move a piece of cargo from one location to another (city to plane or plane to city)
		/// </summary>
		/// <param name="id"></param>
		/// <param name="location"></param>
		/// <returns></returns>
		/// <response code="204">Data updated successful.</response>
		/// <response code="404">Data not found.</response>
		[HttpPut("{id}/location/{location}")]
		[ProducesResponseType(typeof(Plane), 204)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> MoveCargoLocation(string id, string location)
		{
			try
			{
				var result = false;

				var cargo = await this.cargoRepo.GetCargoById(id);

				var planes = await this.planesRepo.GetPlanesAsync();

				var IsNewPlanLocation = planes.Any(x => x.Callsign == location);

				var IsPreviousPlanLocation = planes.Any(x => x.Callsign == cargo.Item2.Location);

				switch (IsNewPlanLocation)
				{
					// Scenario 1 - courier is on loading to a plane from a city
					case true when !IsPreviousPlanLocation:

						var responsePre = await this.cargoRepo.UpdateCargoSourceByLocation(id, location);
						result = responsePre.Item1;
						break;

					// Scenario 2 - courier is offloading to a city
					case false when IsPreviousPlanLocation:
						{
							if (location == cargo.Item2.Destination)
							{
								await this.cargoRepo.DeleteCourierByCargoId(id);
							}
							var response = await this.cargoRepo.UpdateCargoSourceByLocation(id, location);
							result = response.Item1;
							break;
						}
				}

				if (!result)
				{
					return new BadRequestObjectResult("Invalid CargoId");
				}
				return new JsonResult(true);
			}
			catch (ArgumentException exception)
			{
				this.logger.LogError(exception.Message);
				return new NotFoundResult();
			}

		}

		/// <summary>
		/// Get a list of all cargo at a specific location ( by name/id city or plane).
		/// </summary>
		/// <param name="location"></param>
		/// <returns></returns>
		/// <response code="200">Query successful.</response>
		/// <response code="404">Data not found.</response>
		[HttpGet("location/{location}")]
		[ProducesResponseType(typeof(Plane), 200)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> GetCargoAtLocation(string location)
		{
			try
			{
				if (string.IsNullOrEmpty(location))
				{
					return new BadRequestObjectResult("Location is invalid");
				}
				var cargos = await this.cargoRepo.GetAllCargosByLocation(location);
				return new OkObjectResult(cargos.Item2);
			}
			catch (Exception exception)
			{
				this.logger.LogError(exception.Message);
				return new NotFoundResult();
			}
		}

		/// <summary>
		/// Gets average delivery time in seconds
		/// </summary>
		/// <response code="200">Query successful.</response>
		/// <response code="404">Data not found.</response>
		[HttpGet("averageDeliveryTime")]
		[ProducesResponseType(typeof(Plane), 200)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> AverageDeliveryTime()
		{
			try
			{
				var avgTimeInMs = await this.cargoRepo.GetAverageDeliveryTime();
				var avgTimeInSec = avgTimeInMs.Item2 / 1000;
				return new JsonResult(avgTimeInSec);
			}
			catch (Exception exception)
			{
				this.logger.LogError(exception.Message);
				return new NotFoundResult();
			}
		}

		#endregion
	}
}
