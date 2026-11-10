using System.Security.Claims;
using System.Text;
using ChallengeTrackerApi.DTOs.Auth;
using ChallengeTrackerApi.Models;
using ChallengeTrackerApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ChallengeTrackerApi.Services.EF;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;

    public AuthService(UserManager<User> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<(IdentityResult Result, UserResponseDto? User)> RegisterAsync(RegisterRequestDto registerRequestDto)
    {
        var user = new User
        {
            DisplayName = registerRequestDto.DisplayName.Trim(),
            UserName = registerRequestDto.Email,
            Email = registerRequestDto.Email
        };

        var result = await _userManager.CreateAsync(user, registerRequestDto.Password);
        if (!result.Succeeded) return (result, null);

        return (result, new UserResponseDto(user.Id, user.DisplayName, user.Email!));

    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequestDto)
    {
        var user = await _userManager.FindByEmailAsync(loginRequestDto.Email);

        if (user is null || !await _userManager.CheckPasswordAsync(user, loginRequestDto.Password))
            return null;

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(
            _configuration.GetValue("Jwt:ExpiresInMinutes", 60));

        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is missing.");

        var tokenHandler = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            ]),

            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256)
        });

        return new LoginResponseDto(tokenHandler, expiresAt);
    }
}