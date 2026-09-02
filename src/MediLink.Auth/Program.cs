using System.Text;
using MediLink.Core.Interfaces;
using MediLink.Infrastructure.Data;
using MediLink.Infrastructure.Repositories;
using MediLink.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AuthConnection")
    ?? throw new InvalidOperationException("AuthConnection is not configured.");
var mysqlVersion = Version.Parse(builder.Configuration["MySql:ServerVersion"] ?? "8.0.36");
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(mysqlVersion)));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddHttpClient("inventory", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["InventoryService:BaseUrl"] ?? "http://localhost:5201/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? throw new InvalidOperationException("JwtSettings:Secret must be configured.");
if (jwtSecret.Length < 32)
    throw new InvalidOperationException("JwtSettings:Secret must be at least 32 characters long.");
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "MediLinkApi";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "MediLinkClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.WebHost.UseUrls("http://localhost:5101");

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MediLink Auth Service v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/ready", async (AuthDbContext db) =>
{
    try
    {
        return await db.Database.CanConnectAsync() ? Results.Ok(new { status = "ready", service = "MediLink Auth" }) : Results.StatusCode(503);
    }
    catch
    {
        return Results.StatusCode(503);
    }
});

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    if (!bool.TryParse(Environment.GetEnvironmentVariable("MEDILINK_AUTO_CREATE_DATABASE"), out var autoCreate) || autoCreate)
        await db.Database.EnsureCreatedAsync();
    if (!await db.Users.AnyAsync(user => user.Role == MediLink.Core.Enums.UserRole.Admin))
    {
        db.Users.Add(new MediLink.Core.Entities.User
        {
            Email = "admin@medilink.local",
            FirstName = "MediLink",
            LastName = "Admin",
            Role = MediLink.Core.Enums.UserRole.Admin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123")
        });
        await db.SaveChangesAsync();
    }
}

app.Run();
