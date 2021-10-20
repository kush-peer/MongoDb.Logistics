using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDb.Logistics.Models
{
	[BsonIgnoreExtraElements]
	public class Cargo
	{
		[BsonElement("_id")]
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set; }

		[BsonElement("destination")]
		public string Destination { get; set; }

		[BsonElement("location")]
		public string Location { get; set; }

		[BsonElement("courier")]
		public string Courier { get; set; }

		[BsonElement("received")]
		public DateTime Received { get; set; }

		[BsonElement("status")]
		public string Status { get; set; }

		[BsonElement("courierSource")]
		public string CourierSource { get; set; }

		[BsonElement("courierDestination")]
		public string CourierDestination { get; set; }

		[BsonElement("duration")]
		public double Duration { get; set; }

		[BsonElement("deliveredAt")]
		public DateTime DeliveredAt { get; set; }
	}
}
