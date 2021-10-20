
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDb.Logistics.Models;

namespace MongoDb.Logistics.Database.Repositories
{
  /// <summary>
  /// Interfaces to define Operations on Planes Collection
  /// </summary>
  public interface IPlanesRepo
  {
    /// <summary>
    /// Returns All planes 
    /// </summary>
    /// <returns></returns>
    Task<IReadOnlyCollection<Plane>> GetPlanesAsync();

    /// <summary>
    /// Return Plane by Id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<Plane> GetPlaneAsyncById(string id);

    /// <summary>
    /// Update Plane by location and landing 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="location"></param>
    /// <param name="heading"></param>
    /// <param name="landedCity"></param>
    /// <returns></returns>
    Task<bool> UpdatePlaneAsyncByLocationHeadingAndLanded(string id, List<double> location, float heading, string landedCity);

    /// <summary>
    /// Update Plane  By Location
    /// </summary>
    /// <param name="callSign"></param>
    /// <param name="location"></param>
    /// <param name="heading"></param>
    /// <returns></returns>
    Task<bool> UpdatePlaneAsyncByLocationAndHeading(string callSign, List<double> location, float heading);

    /// <summary>
    /// Replace Plane  By location
    /// </summary>
    /// <param name="callSign"></param>
    /// <param name="city"></param>
    /// <returns></returns>
    Task<bool> ReplacePlaneAsyncByRoutes(string callSign, string city);

    /// <summary>
    /// Add Plane  By location
    /// </summary>
    /// <param name="callSign"></param>
    /// <param name="city"></param>
    /// <returns></returns>
    Task<bool> AddPlaneAsyncByRoute(string callSign, string city);

    /// <summary>
    /// Delete  By First Plane Route aka location
    /// </summary>
    /// <param name="callSign"></param>
    /// <returns></returns>
    Task<bool> DeleteAsyncByFirstPlaneRoute(string callSign);

    /// <summary>
    /// Update Mileage And Duration
    /// </summary>
    /// <param name="id"></param>
    /// <param name="landedCity"></param>
    /// <param name="previousCity"></param>
    /// <returns></returns>
    public Task<bool> UpdateMileageAndDuration(string id, City landedCity, City previousCity);
  }
}
