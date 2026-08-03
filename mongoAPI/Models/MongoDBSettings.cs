namespace mongoAPI.Models
{
    public class MongoDBSettings
    {
        public string ConnectionStringURI { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
        public string CollectionName { get; set; } = null!;
    }
}
