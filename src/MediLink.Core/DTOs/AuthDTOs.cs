using System.ComponentModel.DataAnnotations;

namespace MediLink.Core.DTOs;

public class LoginRequest
{
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(128, MinimumLength = 8)] public string Password { get; set; } = string.Empty;
}

public class RegisterCustomerRequest
{
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(128, MinimumLength = 8)] public string Password { get; set; } = string.Empty;
    [Required, StringLength(60)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(60)] public string LastName { get; set; } = string.Empty;
    [Required, StringLength(300)] public string DeliveryAddress { get; set; } = string.Empty;
    [Required, Phone, StringLength(20)] public string PhoneNumber { get; set; } = string.Empty;
}

public class RegisterStoreOwnerRequest
{
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(128, MinimumLength = 8)] public string Password { get; set; } = string.Empty;
    [Required, StringLength(60)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(60)] public string LastName { get; set; } = string.Empty;
    [Required, StringLength(80)] public string BusinessLicenseNumber { get; set; } = string.Empty;
    [Required, StringLength(120)] public string StoreName { get; set; } = string.Empty;
    [Required, StringLength(300)] public string StoreAddress { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
