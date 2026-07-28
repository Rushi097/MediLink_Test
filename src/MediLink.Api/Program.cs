using System.Text;
using MediLink.Core.Interfaces;
using MediLink.Infrastructure.Data;
using MediLink.Infrastructure.Repositories;
using MediLink.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// 1. Database Context Setup (Entity Framework Core)
// -----------------------------------------------------------------------------
// builder.Services.AddDbContext<MediLinkDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddDbContext<MediLinkDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// -----------------------------------------------------------------------------
// 2. Dependency Injection: Repositories & Application Services
// -----------------------------------------------------------------------------
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// -----------------------------------------------------------------------------
// 3. JWT Authentication & Authorization Configuration
// -----------------------------------------------------------------------------
var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "DefaultFallbackSecretKey_PleaseChangeInAppSettings_Minimum32Chars!";
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

// -----------------------------------------------------------------------------
// 4. CORS Policy Configuration
// -----------------------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000") // Vite & CRA defaults
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// -----------------------------------------------------------------------------
// 5. Controllers & OpenAPI / Swagger Configuration
// -----------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MediLink API", Version = "v1" });

    // Enable Authorization header in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// -----------------------------------------------------------------------------
// HTTP Request Pipeline Configuration
// -----------------------------------------------------------------------------
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MediLink API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowReactApp");

// Middleware ordering is critical: Authentication BEFORE Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();