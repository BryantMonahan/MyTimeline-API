using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace mongoAPI.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Username { get; set; }
        public string HashedPassword { get; set; }
        public string Email { get; set; }
        public bool CoolUser { get; set; } = false;
        public int TranscriptionsLeft { get; set; } = 5;

    }
}
