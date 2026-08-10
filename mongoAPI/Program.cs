using mongoAPI.Services;
using DotNetEnv;
using mongoAPI.Types;
using Amazon.S3;
using Amazon;
using Microsoft.Extensions.Options;
using Amazon.Runtime;

var builder = WebApplication.CreateBuilder(args);

Env.Load();
// Get the environment variables from the .env file
bool isProd = Env.GetString("ENVIRONMENT") == "production";
string mongoURI = Env.GetString("MONGODB_URI");
string s3Region = Env.GetString("S3_REGION");
string s3BucketName = Env.GetString("S3_BUCKET_NAME");
string awsAccessKey = Env.GetString("AWS_ACCESS_KEY");
string awsSecretKey = Env.GetString("AWS_SECRET_ACCESS_KEY");

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("DevCorsPolicy", policyBuilder =>
    {
        policyBuilder.AllowAnyHeader().AllowAnyMethod().WithOrigins("http://localhost:4200");
    });
});

builder.Services.Configure<S3Settings>(opt =>
{
    opt.Region = s3Region;
    opt.BucketName = s3BucketName;
});


builder.Services.AddSingleton<MongoDBService>(sp => new MongoDBService(mongoURI));
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var s3Settings = sp.GetRequiredService<IOptions<S3Settings>>().Value;
    var config = new AmazonS3Config
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(s3Settings.Region)
    };

    var creds = new BasicAWSCredentials(awsAccessKey, awsSecretKey);

    return new AmazonS3Client(creds, config);
});
builder.Services.AddSingleton<S3Service>(sp =>
{
    var s3Client = sp.GetRequiredService<IAmazonS3>();
    var s3Settings = sp.GetRequiredService<IOptions<S3Settings>>().Value;

    return new S3Service(s3Client, s3Settings);
});

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
    // Serve and customize Swagger UI 
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "My API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("DevCorsPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();
