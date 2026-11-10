using System.Text;
using ChallengeTrackerApi.Data;
using ChallengeTrackerApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;


namespace ChallengeTrackerApi.Extensions;

public static class IdentityExtensions
{
    public static IServiceCollection AddIdentityServices( this IServiceCollection services)
    {
        services.AddIdentityCore<User>(options => { options.User.RequireUniqueEmail = true;})
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<ApplicationDbContext>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
       
       services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
               .AddJwtBearer(options =>
               {
                   options.MapInboundClaims = false;

                   var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing");

                   options.TokenValidationParameters = new TokenValidationParameters
                   {
                       ValidateIssuerSigningKey = true,
                       IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                       ValidateIssuer = true,
                       ValidIssuer = configuration["Jwt:Issuer"],
                       ValidateAudience = true,
                       ValidAudience = configuration["Jwt:Audience"],
                       ValidateLifetime = true,
                       ClockSkew = TimeSpan.Zero
                   };
               });

       return services;
    }
}
