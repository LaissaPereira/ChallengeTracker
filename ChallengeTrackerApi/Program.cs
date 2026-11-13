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
var databaseProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
        return;
    }

    if (string.Equals(databaseProvider, "SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connectionString, sqlServerOptions =>
            sqlServerOptions.EnableRetryOnFailure());
        return;
    }

    throw new InvalidOperationException(
        $"Unsupported database provider '{databaseProvider}'. Use 'Sqlite' or 'SqlServer'.");
});

// IDENTITY + JWT
builder.Services.AddIdentityServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddApiRateLimiting(builder.Configuration);

//Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IChallengeService, ChallengeService>();
builder.Services.AddScoped<IMembershipService, MembershipService>();
builder.Services.AddScoped<IProgressEntryService, ProgressEntryService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<DbSeeder>();

const string frontendCorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();
if (allowedOrigins is null || allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("Cors: AllowedOrigins must be configured.");
}
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});



//Create the app
var app = builder.Build();


//Middleware configuration (if any) can go here
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(frontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();



app.MapAuthEndpoints();
app.MapChallengeEndpoints();
app.MapMembershipEndpoints();
app.MapProgressEntryEndpoints();
app.MapLeaderboardEndpoints();

// MAP ENDPOINTS - Routes for the API (Development only)
var applyMigrations = app.Environment.IsDevelopment() ||
    builder.Configuration.GetValue<bool>("Database:ApplyMigrations");
var seedData = app.Environment.IsDevelopment() ||
    builder.Configuration.GetValue<bool>("SeedData:Enabled");

if (applyMigrations || seedData)
{
    await using var scope = app.Services.CreateAsyncScope();

    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (applyMigrations)
    {
        await db.Database.MigrateAsync();
    }

    if (seedData)
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // DOC's
    app.Logger.LogInformation("OpenAPI JSON: http://localhost:5040/openapi/v1.json");
    app.Logger.LogInformation("Scalar UI: http://localhost:5040/scalar/v1");
}


app.Run();

public partial class Program { }
