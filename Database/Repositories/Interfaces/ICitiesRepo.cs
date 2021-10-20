using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDb.Logistics.Models;

namespace MongoDb.Logistics.Database.Repositories
{
  /// <summary>
  /// Interfaces to define Operations on cities Collection
  /// </summary>
  public interface ICitiesRepo
  {
    /// <summary>
    /// Get Collection of Cities
    /// </summary>
    /// <returns></returns>
    Task<IReadOnlyCollection<City>> GetCitiesAsync();

    /// <summary>
    /// Get City by Id aka City Name
    /// </summary>
    /// <param name="cityId"></param>
    /// <returns></returns>
    Task<City> GetCityAsyncById(string cityId);

    /// <summary>
    /// Get Nearest City By City Id aka City Name
    /// </summary>
    /// <param name="cityId"></param>
    /// <param name="count" ></param>
    /// <returns></returns>
    Task<NeighborCities> GetNearestCitiesAsync(string cityId, int count);
  }
}
