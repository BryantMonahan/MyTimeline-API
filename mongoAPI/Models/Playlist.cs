using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using System.Text.Json.Serialization;

namespace mongoAPI.Models
{
    public class Playlist
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string username { get; set; } = null!;
        // makes the name in the db items
        [BsonElement("items")]
        // when returning the json, it will be items instead of movieIds
        [JsonPropertyName("items")]
        public List<string> movieIds { get; set; } = null!;
    }
}
