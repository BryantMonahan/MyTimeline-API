using mongoAPI.Services;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

Env.Load();
bool isProd = Env.GetString("ENVIRONMENT") == "production";
string mongoURI = Env.GetString("MONGODB_URI");

builder.Services.AddSingleton<MongoDBService>(sg => new MongoDBService(mongoURI));
var mongoService = builder.Services.BuildServiceProvider().GetRequiredService<MongoDBService>();
await mongoService.AddIndexes();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
