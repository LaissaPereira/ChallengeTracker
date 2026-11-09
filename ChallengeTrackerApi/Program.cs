using ChallengeTrackerApi.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
// DOC's AND VALIDATION
builder.Services.AddOpenApi();

// DATABASE
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing default connection string");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

// MAP ENDPOINTS - Routes for the API (Development only)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // DOC's
    app.Logger.LogInformation("OpenAPI JSON: http://localhost:5262/openapi/v1.json");
    app.Logger.LogInformation("Scalar UI: http://localhost:5262/scalar/v1");
}


app.Run();
