using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MediLink.Core.Entities;
using MediLink.Core.Interfaces;

namespace MediLink.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(User user)
    {
        var secretKey = _config["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("JwtSettings:Secret must be configured.");
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("FullName", $"{user.FirstName} {user.LastName}")
        };
        if (user.StoreOwnerProfile is not null)
            claims.Add(new Claim("StoreOwnerProfileId", user.StoreOwnerProfile.Id.ToString()));

        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"] ?? "MediLinkApi",
            audience: _config["JwtSettings:Audience"] ?? "MediLinkClients",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
