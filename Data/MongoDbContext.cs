using MongoDB.Bson;
using MongoDB.Driver;

namespace EVChargingSystem.WebAPI.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);

            // Try a ping to verify connection
            try
            {
                _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait();
                Console.WriteLine("✅ Successfully connected to MongoDB");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Failed to connect to MongoDB: " + ex.Message);
                throw; // rethrow so the app fails fast
            }
        }


         public IMongoCollection<T> GetCollection<T>(string name)
        {
            return _database.GetCollection<T>(name);
        }
    }
}