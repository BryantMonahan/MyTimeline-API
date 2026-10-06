using mongoAPI.Models;
using mongoAPI.Types;
using MongoDB.Driver;

namespace mongoAPI.Services
{
    public class TranscribeService
    {
        private readonly MongoDBService _mongoDbService;
        private readonly S3Service _s3Service;
        private readonly DeepgramService _deepgramService;
        public TranscribeService(MongoDBService mongoDbService, S3Service s3Service, DeepgramService deepgramService)
        {
            _mongoDbService = mongoDbService;
            _s3Service = s3Service;
            _deepgramService = deepgramService;
        }

        public async Task Transcribe(string ObjectKey, string UserId)
        {
            var filter = Builders<JournalEntry>.Filter.Eq(j => j.UserId, UserId) & Builders<JournalEntry>.Filter.Eq(j => j.ObjectKey, ObjectKey);
            var collection = _mongoDbService.GetJournalCollection();
            var entry = await collection.Find(filter).ToListAsync();
            if (entry.Count == 0)
            {
                throw new Exception("No object with that key belonging to this user was found");
            }
            else if (entry.First().Transcribed != TranscriptionStatus.NotTranscribed)
            {
                throw new Exception("File has already been transcribed");
            }
            // set entry to transcribing
            var update = Builders<JournalEntry>.Update.Set(j => j.Transcribed, TranscriptionStatus.Transcribing);
            await collection.UpdateOneAsync(filter, update);

            var url = _s3Service.GetPresignedUrlGet(ObjectKey);

            var transcript = await _deepgramService.TranscribeFileAsync(url);
            if (transcript.Results.Summary.Result == "success")
            {
                // set as transcribed
                update = Builders<JournalEntry>.Update
                .Set(j => j.Transcribed, TranscriptionStatus.Transcribed)
                .Set(j => j.Transcription, transcript.Results.Channels[0].Alternatives[0].Transcript)
                .Set(j => j.WordCount, transcript.Results.Channels[0].Alternatives[0].Words.Count)
                .Set(j => j.Summary, transcript.Results.Summary.Short);
                await collection.UpdateOneAsync(filter, update);
                var doc = await collection.FindAsync(filter);
                var userCollection = _mongoDbService.GetUserCollection();
                await userCollection.UpdateOneAsync(u => u.Id == UserId, Builders<User>.Update.Inc(u => u.TranscriptionsLeft, -1));
            }
            else
            {
                throw new Exception("The transcript did not succeed");
            }
        }
    }
}