using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using InventoryManagement.Api.Authorization;
using InventoryManagement.Api.Filters;
using InventoryManagement.Api.Middleware;
using InventoryManagement.Api.Services;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace InventoryManagement.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddControllers(o => o.Filters.Add<FluentValidationFilter>())
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // ---- Error handling (RFC 7807)
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // ---- Versioning: /api/v1/...
        services.AddApiVersioning(o =>
            {
                o.DefaultApiVersion = new ApiVersion(1, 0);
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.ReportApiVersions = true;
                o.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(o =>
            {
                o.GroupNameFormat = "'v'VVV";      // "v1" - matches the OpenAPI document name below
                o.SubstituteApiVersionInUrl = true;
            });

        // ---- OpenAPI (built-in) + Swagger UI (see Program.cs)
        services.AddOpenApi("v1", o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        // ---- Authentication / authorization
        services.AddJwtAuthentication(config);
        services.AddAuthorization(Policies.Register);

        // ---- Brute-force protection on credential endpoints
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy("auth", http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });

        services.AddHealthChecks();
        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured from the *validated* JwtSettings options (a missing/short signing key fails startup with a clear
        // message instead of a cryptic crypto exception while the service collection is still being built).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((o, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                o.MapInboundClaims = false;       // keep "sub"/"role" as issued instead of legacy long URN claim names
                o.RequireHttpsMetadata = true;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],   // reject alg confusion / "none"
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };

                o.Events = new JwtBearerEvents
                {
                    // Honour logout immediately: reject access tokens whose jti was denylisted.
                    OnTokenValidated = async context =>
                    {
                        var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                        if (jti is null) { context.Fail("Token has no jti."); return; }

                        var denylist = context.HttpContext.RequestServices.GetRequiredService<ITokenDenylist>();
                        if (await denylist.IsDeniedAsync(jti, context.HttpContext.RequestAborted))
                            context.Fail("Token has been revoked.");
                    }
                };
            });

        return services;
    }
}
