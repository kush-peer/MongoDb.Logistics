using System.Collections.Generic;
using Newtonsoft.Json;

namespace MongoDb.Logistics.Models
{
	public class NeighborCities
	{
		[JsonProperty("neighbors")]
		public List<City> Neighbors { get; set; }
	}
}
