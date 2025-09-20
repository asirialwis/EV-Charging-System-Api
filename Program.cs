using EVChargingSystem.WebAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Text;
using System.Net;
using EVChargingApi.Services;
using EVChargingSystem.WebAPI.Services;
using Swashbuckle.AspNetCore.Filters;
using Microsoft.OpenApi.Models;
using EVChargingSystem.WebAPI.Data.Repositories;
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
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


builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();


// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
   .AddJwtBearer(options =>
   {
       options.TokenValidationParameters = new TokenValidationParameters
       {
           ValidateIssuer = true,
           ValidateAudience = true,
           ValidateLifetime = true,
           ValidateIssuerSigningKey = true,
           ValidIssuer = builder.Configuration["Jwt:Issuer"],
           ValidAudience = builder.Configuration["Jwt:Audience"],
           IssuerSigningKey = new SymmetricSecurityKey(
               Encoding.UTF8.GetBytes(
                   builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.")
               )
           )
       };
   });





// Add services to the container
builder.Services.AddControllers();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Standard Authorization header using the Bearer scheme. Example: \"bearer {token}\"",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    c.OperationFilter<SecurityRequirementsOperationFilter>();
});

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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map health check endpoint
app.MapHealthChecks("/health");

app.Run();
