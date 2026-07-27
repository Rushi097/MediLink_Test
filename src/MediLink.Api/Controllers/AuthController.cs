using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using MediLink.Core.DTOs;
using MediLink.Core.Entities;
using MediLink.Core.Enums;
using MediLink.Core.Interfaces;

namespace MediLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly IJwtTokenService _jwtService;

    public AuthController(IUserRepository userRepo, IJwtTokenService jwtService)
    {
        _userRepo = userRepo;
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            Email = user.Email,
            Role = user.Role.ToString(),
            FullName = $"{user.FirstName} {user.LastName}"
        });
    }

    [HttpPost("register/customer")]
    public async Task<IActionResult> RegisterCustomer([FromBody] RegisterCustomerRequest request)
    {
        if (await _userRepo.UserExistsAsync(request.Email))
            return BadRequest(new { message = "Email is already registered." });

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.Customer
        };

        var profile = new CustomerProfile
        {
            DeliveryAddress = request.DeliveryAddress,
            PhoneNumber = request.PhoneNumber
        };

        await _userRepo.CreateCustomerAsync(user, profile);
        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            Email = user.Email,
            Role = user.Role.ToString(),
            FullName = $"{user.FirstName} {user.LastName}"
        });
    }

    [HttpPost("register/store-owner")]
    public async Task<IActionResult> RegisterStoreOwner([FromBody] RegisterStoreOwnerRequest request)
    {
        if (await _userRepo.UserExistsAsync(request.Email))
            return BadRequest(new { message = "Email is already registered." });

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.StoreOwner
        };

        var profile = new StoreOwnerProfile
        {
            BusinessLicenseNumber = request.BusinessLicenseNumber
        };

        var store = new Store
        {
            Name = request.StoreName,
            Address = request.StoreAddress
        };

        await _userRepo.CreateStoreOwnerAsync(user, profile, store);
        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            Email = user.Email,
            Role = user.Role.ToString(),
            FullName = $"{user.FirstName} {user.LastName}"
        });
    }
}
