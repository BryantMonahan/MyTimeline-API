using mongoAPI.Services;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("CorsPolicy", policyBuilder =>
    {
        policyBuilder.AllowAnyHeader().AllowAnyMethod().WithOrigins("http://localhost:4200");
    });
});

// Get the environment variables from the .env file
Env.Load();
bool isProd = Env.GetString("ENVIRONMENT") == "production";
string mongoURI = Env.GetString("MONGODB_URI");

builder.Services.AddSingleton<MongoDBService>(sg => new MongoDBService(mongoURI));

// Ensure indexes are created before the application starts
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

app.UseCors("CorsPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();
