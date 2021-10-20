using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDb.Logistics.Models
{

	[BsonIgnoreExtraElements]
	public class Plane
	{
		[BsonElement("_id")]
		[BsonId]
		public string Callsign { get; set; }

		[BsonElement("currentLocation")]
		public List<double> CurrentLocation { get; set; }

		[BsonElement("heading")]
		public decimal? Heading { get; set; }

		[BsonElement("route")]
		public List<string> Route { get; set; }

		[BsonElement("landed")]
		public string Landed { get; set; }

		[BsonElement("lastLanded")]
		public string LastLanded { get; set; }

		[BsonElement("currentDateTimeStamp")]
		public DateTime CurrentDateTimeStamp { get; set; }

		[BsonElement("lastDateTimeStamp")]
		public DateTime? LastDateTimeStamp { get; set; }

		[BsonElement("TotalMileage")]
		public int? TotalMileage { get; set; }

		[BsonElement("totalDuration")]
		public double? TotalDuration { get; set; }

		[BsonElement("distanceCoveredInMiles")]
		public double? DistanceCoveredInMiles { get; set; }

		[BsonElement("isMaintenanceRequired")]
		public bool IsMaintenanceRequired { get; set; }
	}

}
