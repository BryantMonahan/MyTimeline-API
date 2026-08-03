using mongoAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;

namespace mongoAPI.Services
{
    public class MongoDBService
    {
        private readonly IMongoCollection<Playlist> _playlistCollection;

        public MongoDBService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            MongoClient client = new MongoClient(mongoDBSettings.Value.ConnectionStringURI);
            IMongoDatabase db = client.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _playlistCollection = db.GetCollection<Playlist>(mongoDBSettings.Value.CollectionName);
        }

        public async Task<List<Playlist>> GetAllPlaylists()
        {
            var playlists = await _playlistCollection.FindAsync(_ => true);
            return playlists.ToList();
        }

        public async Task CreatePlaylist(Playlist playlist)
        {
            await _playlistCollection.InsertOneAsync(playlist);
        }

        public async Task DeletePlaylist(string id)
        {
            await _playlistCollection.DeleteOneAsync(p => p.Id == id);
        }

        public async Task AddPlaylist(string id, string movieId)
        {
            await _playlistCollection.UpdateOneAsync(p => p.Id == id, Builders<Playlist>.Update.AddToSet<string>("movieId", movieId));
        }
    }
}
