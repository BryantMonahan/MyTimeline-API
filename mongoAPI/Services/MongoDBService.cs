using mongoAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;

namespace mongoAPI.Services
{
    public class MongoDBService
    {
        private readonly IMongoCollection<User> usersCollection;
        private readonly IMongoDatabase _mongoDb;

        public MongoDBService(string connectionURI)
        {
            MongoClient client = new MongoClient(connectionURI);
            _mongoDb = client.GetDatabase("my-timeline-db");
            usersCollection = _mongoDb.GetCollection<User>("users");
        }

        /**
         * Creates unique indexes for the username and email fields in the users collection.
         * This ensures that no two users can have the same username or email.
         */
        public async Task AddIndexes()
        {
            var userNameIndexKey = Builders<User>.IndexKeys.Ascending(u => u.username);
            var emailIndexKey = Builders<User>.IndexKeys.Ascending(u => u.email);
            var indexOptions = new CreateIndexOptions { Unique = true };
            var userNameIndexModel = new CreateIndexModel<User>(userNameIndexKey, indexOptions);
            var emailIndexModel = new CreateIndexModel<User>(emailIndexKey, indexOptions);
            //var collection  = await _mongoDb.GetCollection<User>("Users")
            await _mongoDb.GetCollection<User>("users").Indexes.CreateManyAsync(new List<CreateIndexModel<User>> { userNameIndexModel, emailIndexModel });
        }
        public IMongoCollection<User> GetUserCollection()
        {
            return usersCollection;
        }

        //public async Task<List<Playlist>> GetAllPlaylists()
        //{
        //    var playlists = await _playlistCollection.FindAsync(_ => true);
        //    return playlists.ToList();
        //}

        //public async Task CreatePlaylist(Playlist playlist)
        //{
        //    await _playlistCollection.InsertOneAsync(playlist);
        //}

        //public async Task DeletePlaylist(string id)
        //{
        //    await _playlistCollection.DeleteOneAsync(p => p.Id == id);
        //}

        //public async Task AddPlaylist(string id, string movieId)
        //{
        //    await _playlistCollection.UpdateOneAsync(p => p.Id == id, Builders<Playlist>.Update.AddToSet<string>("movieId", movieId));
        //}
    }
}
