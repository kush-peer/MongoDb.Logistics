using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDb.Logistics.Constants;

namespace MongoDb.Logistics.Models
{
	public class City
	{
		[BsonElement(MongoDbConstant.Id)]
		[BsonId]
		public string Name { get; set; }

		[BsonElement("country")]
		public string Country { get; set; }

		[BsonElement("position")]
		public List<double> Location { get; set; }
	}
}
