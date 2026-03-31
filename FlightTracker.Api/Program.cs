using FlightTracker.Application.Mapping;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Application.Services.Implementation.Analytics;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Application.Services.Interfaces.Analytics;
using FlightTracker.Api.Infrastructure;
using FlightTracker.Infrastructure.Data;
using FlightTracker.Infrastructure.External;
using FlightTracker.Infrastructure.Repositories.Implementation;
using FlightTracker.Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Polly;
using Polly.Contrib.WaitAndRetry;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FlightTracker API",
        Version = "v1",
        Description = """
            ## Authentication

            Most endpoints require a Bearer token:

            ```
            Authorization: Bearer {token}
            ```

            **Blazor Web frontend** — short-lived first-party JWTs issued by the Web host.

            **External clients** — Personal Access Tokens (PAT) created via
            `POST /api/v1/users/{userId}/access-tokens`. PATs use the `ft_pat_*` prefix.

            ## Rate Limits

            | Client type | Limit |
            |---|---|
            | PAT (`ft_pat_*`) | 120 requests / minute |
            | IP fallback | 600 requests / minute |

            Returns `429 Too Many Requests` when exceeded.

            ## PAT Scopes

            | Scope | Access |
            |---|---|
            | `read:flights` | Read user flight history |
            | `write:flights` | Create and modify flight records |
            | `read:stats` | Read statistics and passport data |

            ## Versioning

            Route-based versioning. Additive changes ship within the same major version.
            Breaking changes get a new prefix (e.g. `/api/v2`).
            Deprecated versions include `api-supported-versions` and
            `api-deprecation-notes` response headers.

            ## Ownership

            User-scoped endpoints enforce that the `userId` route parameter matches
            the authenticated token subject. Returns `403` on mismatch.
            """
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = """
            Enter your bearer token.

            **First-party clients**: JWT issued by the FlightTracker Web host.

            **External clients**: Personal Access Token with prefix `ft_pat_*`.

            Example: `Bearer ft_pat_abc123...`
            """
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer")] = new List<string>()
    });

    var xmlFile = $"{System.Reflection.Assembly
        .GetExecutingAssembly()
        .GetName()
        .Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var bearerSection = builder.Configuration.GetSection("Authentication:Bearer");
var authority = bearerSection["Authority"];
var audience = bearerSection["Audience"];
var issuer = bearerSection["Issuer"];
var signingKey = bearerSection["SigningKey"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata =
            bearerSection.GetValue("RequireHttpsMetadata", true);

        if (!string.IsNullOrWhiteSpace(authority))
        {
            options.Authority = authority;
        }

        if (!string.IsNullOrWhiteSpace(audience))
        {
            options.Audience = audience;
        }

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
            ValidIssuer = issuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(audience),
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.NameIdentifier
        };

        if (string.IsNullOrWhiteSpace(authority) &&
            !string.IsNullOrWhiteSpace(signingKey))
        {
            tokenValidationParameters.ValidateIssuerSigningKey = true;
            tokenValidationParameters.IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        }

        options.TokenValidationParameters = tokenValidationParameters;
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var authorizationHeader = context.Request.Headers.Authorization.ToString();
        if (authorizationHeader.StartsWith("Bearer ft_pat_", StringComparison.OrdinalIgnoreCase))
        {
            var tokenPartition = authorizationHeader["Bearer ".Length..].Trim();
            return RateLimitPartition.GetFixedWindowLimiter(
                tokenPartition,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        }

        var fallbackPartition = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            fallbackPartition,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 600,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

builder.Services.AddDbContext<FlightTrackerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")
        ?? "Data Source=../flighttracker.dev.db"));

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<FlightTrackerDbContext>();

builder.Services.AddScoped<IAirportRepository, AirportRepository>();
builder.Services.AddScoped<IFlightRepository, FlightRepository>();
builder.Services.AddScoped<IUserFlightRepository, UserFlightRepository>();
builder.Services.AddScoped<IAircraftRepository, AircraftRepository>();
builder.Services.AddScoped<IAirlineRepository, AirlineRepository>();
builder.Services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();
builder.Services.AddScoped<IPersonalAccessTokenRepository, PersonalAccessTokenRepository>();

builder.Services.AddSingleton<IClock, UtcClock>();
builder.Services.AddScoped<IAirportService, AirportService>();
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IUserFlightService, UserFlightService>();
builder.Services.AddScoped<IMapFlightService, MapFlightService>();
builder.Services.AddScoped<IFlightStatsService, FlightStatsService>();
builder.Services.AddScoped<IPassportService, PassportService>();
builder.Services.AddScoped<IAirportOverviewService, AirportOverviewService>();
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
builder.Services.AddScoped<IPersonalAccessTokenService, PersonalAccessTokenService>();
builder.Services.AddScoped<IUsernameValidationService, UsernameValidationService>();
builder.Services.AddScoped<IAirportEnrichmentService, AirportEnrichmentService>();
builder.Services.AddScoped<IFlightMetadataProvisionService, FlightMetadataProvisionService>();
builder.Services.AddSingleton<IDistanceCalculator, DistanceCalculator>();

builder.Services.AddHttpClient<ITimeApiService, TimeApiService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(3);
});

builder.Services.AddHttpClient<IAirportLiveService, AviationstackService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(6);
})
.AddPolicyHandler(
    Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .OrResult(r => (int)r.StatusCode is >= 500 or 429)
        .WaitAndRetryAsync(
            Backoff.DecorrelatedJitterBackoffV2(
                TimeSpan.FromMilliseconds(200),
                3)));

builder.Services.AddHttpClient<AdsBdbClient>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(6);
});
builder.Services.AddScoped<IFlightRouteLookupClient>(sp => sp.GetRequiredService<AdsBdbClient>());
builder.Services.AddScoped<IAircraftLookupClient>(sp => sp.GetRequiredService<AdsBdbClient>());
builder.Services.AddScoped<IAirlineLookupClient>(sp => sp.GetRequiredService<AdsBdbClient>());

builder.Services.AddHttpClient<AirportDbClient>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(6);
});
builder.Services.AddScoped<IAirportLookupClient>(sp => sp.GetRequiredService<AirportDbClient>());

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<ApplicationMappingProfile>();
    cfg.AddProfile<FlightProfile>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FlightTracker API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Headers["api-supported-versions"] = "1.0";
        context.Response.Headers["api-deprecation-notes"] =
            "See /swagger for versioning and deprecation policy.";
    }

    await next();
});

app.MapGet("/api/versioning", () => Results.Ok(new
{
    currentVersion = "v1",
    supportedVersions = new[] { "v1" },
    deprecatedVersions = Array.Empty<string>(),
    documentation = "/swagger"
}));

// Development seed (only if DB empty)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FlightTrackerDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    try
    {
        await SeedData.EnsureSeededAsync(db, userManager);
        Console.WriteLine("[Startup] Development database ensured & seeded (SQLite)");
    }
    catch (Exception se)
    {
        Console.WriteLine($"[Startup] Seed error: {se}");
    }
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseMiddleware<TokenAuditMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();
