using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using BCrypt.Net;
using MediLink.Core.DTOs;
using MediLink.Core.Entities;
using MediLink.Core.Enums;
using MediLink.Core.Interfaces;

namespace MediLink.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly IJwtTokenService _jwtService;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(IUserRepository userRepo, IJwtTokenService jwtService, IHttpClientFactory httpClientFactory)
    {
        _userRepo = userRepo;
        _jwtService = jwtService;
        _httpClientFactory = httpClientFactory;
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
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            FullName = $"{user.FirstName} {user.LastName}",
            PhoneNumber = user.CustomerProfile?.PhoneNumber,
            DeliveryAddress = user.CustomerProfile?.DeliveryAddress
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
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            FullName = $"{user.FirstName} {user.LastName}",
            PhoneNumber = profile.PhoneNumber,
            DeliveryAddress = profile.DeliveryAddress
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

        await _userRepo.CreateStoreOwnerAsync(user, profile);

        try
        {
            var client = _httpClientFactory.CreateClient("inventory");
            using var inventoryRequest = new HttpRequestMessage(HttpMethod.Post, "api/stores/internal");
            inventoryRequest.Headers.TryAddWithoutValidation("X-MediLink-Internal-Key", InternalKey());
            inventoryRequest.Content = JsonContent.Create(new
            {
                Name = request.StoreName.Trim(),
                Address = request.StoreAddress.Trim(),
                StoreOwnerProfileId = profile.Id
            });
            using var inventoryResponse = await client.SendAsync(inventoryRequest);
            if (!inventoryResponse.IsSuccessStatusCode)
            {
                await _userRepo.DeleteUserAsync(user.Id);
                return StatusCode(StatusCodes.Status502BadGateway, new { message = "Store account was not created because the Inventory service is unavailable." });
            }
        }
        catch
        {
            await _userRepo.DeleteUserAsync(user.Id);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Store account was not created because the Inventory service is unavailable." });
        }

        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            FullName = $"{user.FirstName} {user.LastName}"
        });
    }
    [HttpGet("internal/stats")]
    public async Task<IActionResult> InternalStats()
    {
        if (!IsInternalRequest()) return Unauthorized(new { success = false, message = "Internal service authentication required." });
        return Ok(new { success = true, customers = await _userRepo.CountByRoleAsync(UserRole.Customer), pharmacyOwners = await _userRepo.CountByRoleAsync(UserRole.StoreOwner) });
    }

    [HttpGet("internal/users/{id:guid}")]
    public async Task<IActionResult> InternalUser(Guid id)
    {
        if (!IsInternalRequest()) return Unauthorized(new { success = false, message = "Internal service authentication required." });
        var user = await _userRepo.GetByIdAsync(id);
        if (user is null) return NotFound(new { success = false, message = "User not found." });
        return Ok(new
        {
            success = true,
            user = new { user.Id, user.Email, user.FirstName, user.LastName, Role = user.Role.ToString() },
            customerProfile = user.CustomerProfile is null ? null : new { user.CustomerProfile.PhoneNumber, user.CustomerProfile.DeliveryAddress },
            storeOwnerProfile = user.StoreOwnerProfile is null ? null : new { user.StoreOwnerProfile.Id, user.StoreOwnerProfile.BusinessLicenseNumber }
        });
    }

    private string InternalKey() => Environment.GetEnvironmentVariable("MEDILINK_INTERNAL_KEY") ?? "medilink-internal-development-key";
    private bool IsInternalRequest() => Request.Headers.TryGetValue("X-MediLink-Internal-Key", out var key) && key == InternalKey();
}
