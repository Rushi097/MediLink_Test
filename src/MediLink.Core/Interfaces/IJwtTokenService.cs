using MediLink.Core.Entities;

namespace MediLink.Core.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}