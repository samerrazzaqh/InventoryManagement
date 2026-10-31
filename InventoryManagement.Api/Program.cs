using InventoryManagement.Api.Extensions;
using InventoryManagement.Api.Middleware;
using InventoryManagement.Application;
using InventoryManagement.Infrastructure;
using InventoryManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

// Order matters: logging wraps everything (sees the final status code), then the exception handler.
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    
    // ضبط إعدادات Swagger UI للعمل بشكل صحيح مع .NET OpenAPI
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Inventory API v1");
        options.RoutePrefix = "swagger"; // جعل واجهة السواجر تظهر مباشرة على /swagger
    });
}
else
{
    app.UseHsts();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live").AllowAnonymous();

// Roles are always ensured; migrations are applied automatically in Development only.
await DbInitializer.InitialiseAsync(app.Services, applyMigrations: app.Environment.IsDevelopment());

await app.RunAsync();

public partial class Program;   // exposes the entry point to WebApplicationFactory<Program> in integration tests
