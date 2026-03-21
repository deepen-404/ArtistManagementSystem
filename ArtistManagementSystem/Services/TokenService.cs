using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ArtistManagementSystem.Models;
using Microsoft.IdentityModel.Tokens;

namespace ArtistManagementSystem.Services;

public class TokenService(IConfiguration configuration)
{
    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? "DefaultSecretKey12345"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName)
        };

        if (user.Role != UserRole.artist)
        {
            claims.Add(new Claim(ClaimTypes.Role, user.Role.ToString()));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.Role, "artist"));
        }

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "ArtistManagementSystem",
            audience: configuration["Jwt:Audience"] ?? "ArtistManagementSystem",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
