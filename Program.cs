using System.Reflection;
using Amazon.Runtime;
using Amazon.S3;
using Coflnet.Auth;
using Coflnet.Core;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "CtwApi",
                    Version = "v1",
                    Description = ""
                });
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath, true);
            });
builder.Services.AddControllers();
builder.AddCoflAuthService();
builder.Services.AddCoflnetCore();
builder.Services.AddSingleton<ImagesService>();
builder.Services.AddSingleton<ObjectService>();
builder.Services.AddSingleton<IAmazonS3>(sb =>
{
    AmazonS3Config awsCofig = new AmazonS3Config
    {
        ServiceURL = builder.Configuration["S3_BASE_URL"]
    };
    var credentials = new BasicAWSCredentials(builder.Configuration["ACCESS_KEY"], builder.Configuration["SECRET_KEY"]);

    return new AmazonS3Client(
            credentials,
            awsCofig
            );
});

var app = builder.Build();

app.UseSwagger(c =>
{
    c.RouteTemplate = "api/openapi/{documentName}/openapi.json";
})
.UseSwaggerUI(c =>
{
    c.RoutePrefix = "api";
    c.SwaggerEndpoint("/api/openapi/v1/openapi.json", "Ctw");
});

app.UseCoflnetCore();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
