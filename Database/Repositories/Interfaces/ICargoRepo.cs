using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDb.Logistics.Models;

namespace MongoDb.Logistics.Database.Repositories.Interfaces
{
	/// <summary>
	/// Interfaces to define Operations on Planes Collection
	/// </summary>
	public interface ICargoRepo
	{
		/// <summary>
		/// Create a new cargo at "location" which needs to get to "destination"
		/// </summary>
		/// <param name="location"></param>
		/// <param name="destination"></param>
		/// <returns></returns>
		Task<(bool, Cargo)> CreateNewCargo(string location, string destination);

		/// <summary>
		/// Update Cargo status field to 'Delivered' and delivered date
		/// </summary>
		/// <param name="cargo"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		Task<(bool, long)> UpdateCargoStatusAndDuration(Cargo cargo, string status);

		/// <summary>
		/// Update Cargo Courier 
		/// </summary>
		/// <param name="cargoId"></param>
		/// <param name="courier"></param>
		/// <returns></returns>
		Task<(bool, long)> UpdateCargoCourierByCargoId(string cargoId, string courier);

		/// <summary>
		/// Update the value of courier on a given piece of cargo to null
		/// </summary>
		/// <param name="cargoId"></param>
		/// <returns></returns>
		Task<(bool, long)> DeleteCourierByCargoId(string cargoId);

		/// <summary>
		/// Get a cargo details by cargo Id
		/// </summary>
		/// <param name="cargoId"></param>
		/// <returns></returns>
		Task<(bool, Cargo)> GetCargoById(string cargoId);

		/// <summary>
		/// Get a list of all cargo at a specific location ( by name/id city or plane).
		/// </summary>
		/// <param name="location"></param>
		/// <returns></returns>
		Task<(bool, IReadOnlyList<Cargo>)> GetAllCargosByLocation(string location);

		/// <summary>
		/// Update Cargo Source By Location
		/// </summary>
		/// <param name="cargoId"></param>
		/// <param name="location"></param>
		/// <returns></returns>
		Task<(bool, long)> UpdateCargoSourceByLocation(string cargoId, string location);

		/// <summary>
		/// Get Average Delivery Time
		/// </summary>
		/// <returns></returns>
		Task<(bool, double)> GetAverageDeliveryTime();

	}
}
