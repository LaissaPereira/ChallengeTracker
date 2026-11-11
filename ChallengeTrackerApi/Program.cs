using System.Text.Json;
using System.Text.Json.Serialization;
using ChallengeTrackerApi.Data;
using ChallengeTrackerApi.Endpoints;
using ChallengeTrackerApi.Errors;
using ChallengeTrackerApi.Extensions;
using ChallengeTrackerApi.Services.Interfaces;
using ChallengeTrackerApi.Services.EF;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
// DOC's AND VALIDATION
builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// STRING <=> ENUM CONVERTER
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
});

// DATABASE
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing default connection string");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// IDENTITY + JWT
builder.Services.AddIdentityServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddApiRateLimiting(builder.Configuration);

//Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IChallengeService, ChallengeService>();
builder.Services.AddScoped<DbSeeder>();


//Create the app
var app = builder.Build();


//Middleware configuration (if any) can go here
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();



app.MapAuthEndpoints();
app.MapChallengeEndpoints();

// MAP ENDPOINTS - Routes for the API (Development only)
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();

    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();

    app.MapOpenApi();
    app.MapScalarApiReference();

    // DOC's
    app.Logger.LogInformation("OpenAPI JSON: http://localhost:5040/openapi/v1.json");
    app.Logger.LogInformation("Scalar UI: http://localhost:5040/scalar/v1");
}


app.Run();

public partial class Program { }
