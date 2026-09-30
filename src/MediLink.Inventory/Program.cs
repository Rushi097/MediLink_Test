using System.Text;
using MediLink.Inventory.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5201");

var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? Environment.GetEnvironmentVariable("MEDILINK_JWT_SECRET") ?? "medilink-development-secret-must-be-32-characters";
if (jwtSecret.Length < 32) throw new InvalidOperationException("JwtSettings:Secret must be at least 32 characters long.");
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "MediLinkApi";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "MediLinkClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
    ValidIssuer = jwtIssuer, ValidAudience = jwtAudience,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
});

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options.AddPolicy("AllowReactApp", policy =>
    policy.WithOrigins("http://localhost:5173", "http://localhost:3000").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddHttpClient("medicineCatalog", client =>
{
    client.BaseAddress = new Uri("https://rxnav.nlm.nih.gov/REST/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MediLink/1.0 (CDAC project)");
});

builder.Services.AddSingleton<FlatFileInventoryStore>();
builder.Services.AddSingleton<IMedicineCatalogService, MedicineCatalogService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MediLink Inventory Service v2 - Flat File"));
}
app.UseStaticFiles();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/ready", (FlatFileInventoryStore files) => Results.Ok(new { status = "ready", service = "MediLink Inventory", storage = "flat-file-json" }));

await app.Services.GetRequiredService<FlatFileInventoryStore>().InitializeAsync();
app.Run();
