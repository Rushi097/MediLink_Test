using System.ComponentModel.DataAnnotations;
using MediLink.Api.Controllers;
using MediLink.Core.DTOs;
using MediLink.Core.Entities;
using MediLink.Core.Enums;
using MediLink.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MediLink.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_returns_unauthorized_when_password_is_incorrect()
    {
        var repository = new FakeUserRepository(new User
        {
            Email = "owner@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct@123"),
            Role = UserRole.StoreOwner
        });
        var controller = new AuthController(repository, new FakeJwtTokenService());

        var result = await controller.Login(new LoginRequest { Email = "owner@example.com", Password = "Wrong@123" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_returns_token_and_user_details_when_credentials_are_valid()
    {
        var user = new User
        {
            Email = "customer@example.com",
            FirstName = "Asha",
            LastName = "Patel",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct@123"),
            Role = UserRole.Customer
        };
        var controller = new AuthController(new FakeUserRepository(user), new FakeJwtTokenService());

        var result = await controller.Login(new LoginRequest { Email = user.Email, Password = "Correct@123" });

        var response = Assert.IsType<OkObjectResult>(result).Value as AuthResponse;
        Assert.NotNull(response);
        Assert.Equal("test-token", response.Token);
        Assert.Equal("Customer", response.Role);
        Assert.Equal("Asha Patel", response.FullName);
    }

    [Fact]
    public void Registration_request_requires_a_password_with_at_least_eight_characters()
    {
        var request = new RegisterCustomerRequest
        {
            Email = "customer@example.com", Password = "short", FirstName = "Asha", LastName = "Patel",
            DeliveryAddress = "Pune", PhoneNumber = "9876543210"
        };
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(RegisterCustomerRequest.Password)));
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public string GenerateToken(User user) => "test-token";
    }

    private sealed class FakeUserRepository(User? initialUser) : IUserRepository
    {
        private readonly User? user = initialUser;
        public Task<User?> GetByEmailAsync(string email) => Task.FromResult(user?.Email == email ? user : null);
        public Task<User?> GetByIdAsync(Guid id) => Task.FromResult(user?.Id == id ? user : null);
        public Task<User> CreateCustomerAsync(User user, CustomerProfile profile) => Task.FromResult(user);
        public Task<User> CreateStoreOwnerAsync(User user, StoreOwnerProfile profile, Store store) => Task.FromResult(user);
        public Task<bool> UserExistsAsync(string email) => Task.FromResult(user?.Email == email);
    }
}
