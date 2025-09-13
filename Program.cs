using EVChargingSystem.WebAPI.Data;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Host.ConfigureLogging(logging =>
{
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Debug);
});

// Get the connection string and database name from configuration
var connectionString = builder.Configuration.GetValue<string>("EVChargingDatabase:ConnectionString");
var databaseName = builder.Configuration.GetValue<string>("EVChargingDatabase:DatabaseName");

// Register the MongoDbContext as a singleton service
builder.Services.AddSingleton(new MongoDbContext(connectionString, databaseName));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add health checks for MongoDB
builder.Services.AddHealthChecks()
    .AddMongoDb(
        sp => new MongoClient(connectionString), 
        name: "mongodb",
        timeout: TimeSpan.FromSeconds(5),
        tags: new[] { "db", "mongo" });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// Map health check endpoint
app.MapHealthChecks("/health");

app.Run();
