using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace mongoAPI.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string username { get; set; }
        public string hashedPassword { get; set; }
        public string email { get; set; }
        public bool coolUser { get; set; } = false;

    }
}
