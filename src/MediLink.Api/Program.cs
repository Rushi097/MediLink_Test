using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5140");
builder.Services.AddHttpClient("gateway", client => { client.Timeout = TimeSpan.FromSeconds(30); });
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

var app = builder.Build();
app.UseCors("AllowReactApp");

var routes = new (string Prefix, string Target)[]
{
    ("/api/auth", builder.Configuration["Services:Auth"] ?? "http://localhost:5101"),
    ("/api/medicines", builder.Configuration["Services:Inventory"] ?? "http://localhost:5201"),
    ("/api/stores", builder.Configuration["Services:Inventory"] ?? "http://localhost:5201"),
    ("/api/cart", builder.Configuration["Services:Order"] ?? "http://localhost:5301"),
    ("/api/orders", builder.Configuration["Services:Order"] ?? "http://localhost:5301"),
    ("/api/dashboard", builder.Configuration["Services:Order"] ?? "http://localhost:5301"),
    ("/api/portal", builder.Configuration["Services:Order"] ?? "http://localhost:5301"),
    ("/api/inventory", builder.Configuration["Services:Order"] ?? "http://localhost:5301"),
    ("/api/store-orders", builder.Configuration["Services:Order"] ?? "http://localhost:5301")
};

app.MapGet("/health", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("gateway");
    var checks = new Dictionary<string, string>();
    var healthy = true;
    foreach (var service in new[] {
        ("auth", (builder.Configuration["Services:Auth"] ?? "http://localhost:5101") + "/ready"),
        ("inventory", (builder.Configuration["Services:Inventory"] ?? "http://localhost:5201") + "/ready"),
        ("order", (builder.Configuration["Services:Order"] ?? "http://localhost:5301") + "/ready")
    })
    {
        try
        {
            using var response = await client.GetAsync(service.Item2);
            checks[service.Item1] = response.IsSuccessStatusCode ? "healthy" : $"unhealthy ({(int)response.StatusCode})";
            healthy &= response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            checks[service.Item1] = $"unreachable: {ex.Message}";
            healthy = false;
        }
    }
    return Results.Json(new { status = healthy ? "healthy" : "degraded", services = checks }, statusCode: healthy ? 200 : 503);
});

app.MapGet("/", () => Results.Ok(new { service = "MediLink API Gateway", status = "running" }));
app.MapGet("/swagger", () => Results.Redirect("http://localhost:5101/swagger"));
app.MapGet("/swagger/index.html", () => Results.Redirect("http://localhost:5101/swagger"));

app.MapMethods("/api/{**path}", new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS" }, async (HttpContext context, IHttpClientFactory factory) =>
{
    if (HttpMethods.IsOptions(context.Request.Method))
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }

    var path = context.Request.Path.Value ?? "/";
    var route = routes.FirstOrDefault(r => path.StartsWith(r.Prefix, StringComparison.OrdinalIgnoreCase));
    if (route == default)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(new { success = false, message = "No service route is configured for this endpoint." });
        return;
    }

    var target = route.Target + path + context.Request.QueryString;
    var client = factory.CreateClient("gateway");
    using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), target);

    if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
        request.Content = new StreamContent(context.Request.Body);

    foreach (var header in context.Request.Headers)
    {
        if (string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase)) continue;
        if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
    }

    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
    context.Response.StatusCode = (int)response.StatusCode;

    foreach (var header in response.Headers)
        context.Response.Headers[header.Key] = header.Value.ToArray();
    foreach (var header in response.Content.Headers)
        context.Response.Headers[header.Key] = header.Value.ToArray();
    context.Response.Headers.Remove("transfer-encoding");

    await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
});

app.Run();
