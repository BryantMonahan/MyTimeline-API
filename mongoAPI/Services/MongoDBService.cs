using mongoAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;

namespace mongoAPI.Services
{
    public class MongoDBService
    {
        private readonly IMongoCollection<User> usersCollection;
        private readonly IMongoCollection<JournalEntry> journalCollection;
        private readonly IMongoDatabase _mongoDb;
        private readonly MongoClient _client;

        public MongoDBService(string connectionURI)
        {
            _client = new MongoClient(connectionURI);
            _mongoDb = _client.GetDatabase("my-timeline-db");
            usersCollection = _mongoDb.GetCollection<User>("users");
            journalCollection = _mongoDb.GetCollection<JournalEntry>("journalEntries");
        }

        /**
         * Creates unique indexes for the username and email fields in the users collection.
         * This ensures that no two users can have the same username or email.
         */
        public async Task AddIndexes()
        {
            var userNameIndexKey = Builders<User>.IndexKeys.Ascending(u => u.Username);
            var emailIndexKey = Builders<User>.IndexKeys.Ascending(u => u.Email);
            var indexOptions = new CreateIndexOptions { Unique = true };
            var userNameIndexModel = new CreateIndexModel<User>(userNameIndexKey, indexOptions);
            var emailIndexModel = new CreateIndexModel<User>(emailIndexKey, indexOptions);
            await _mongoDb.GetCollection<User>("users").Indexes.CreateManyAsync(new List<CreateIndexModel<User>> { userNameIndexModel, emailIndexModel });
        }
        public IMongoCollection<User> GetUserCollection()
        {
            return usersCollection;
        }

        public IMongoCollection<JournalEntry> GetJournalCollection()
        {
            return journalCollection;
        }

        // public async Task<IClientSessionHandle> GetSession()
        // {
        //     return await _client.StartSessionAsync();
        // }
    }
}
