using System.ComponentModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using mongoAPI.Types;

namespace mongoAPI.Models
{
    public class JournalEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }
        public string ObjectKey { get; set; }
        public DateTime? Created { get; set; }
        public DateTime Uploaded { get; set; }
        public string Name { get; set; }
        public string FileName { get; set; }
        public string Description { get; set; } = "";
        public int SecLength { get; set; }
        public int SizeInBytes { get; set; }
        public string? Transcription { get; set; }
        public bool Validated { get; set; } = false;
        public TranscriptionStatus Transcribed { get; set; } = TranscriptionStatus.NotTranscribed;
    }
}