using FlightTracker.Application.Mapping;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Application.Services.Implementation.Analytics;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Application.Services.Interfaces.Analytics;
using FlightTracker.Infrastructure.Data;
using FlightTracker.Infrastructure.External;
using FlightTracker.Infrastructure.Repositories.Implementation;
using FlightTracker.Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Polly;
using Polly.Contrib.WaitAndRetry;

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
        Description = "Versioning and deprecation policy: see doc/ApiVersioningPolicy.md"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Use: Authorization: Bearer {token}"
    });
});

var bearerSection = builder.Configuration.GetSection("Authentication:Bearer");
var authority = bearerSection["Authority"];
var audience = bearerSection["Audience"];
var issuer = bearerSection["Issuer"];

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

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
            ValidIssuer = issuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(audience),
            ValidAudience = audience
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<FlightTrackerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")
        ?? "Data Source=flighttracker.dev.db"));

builder.Services.AddScoped<IAirportRepository, AirportRepository>();
builder.Services.AddScoped<IFlightRepository, FlightRepository>();
builder.Services.AddScoped<IUserFlightRepository, UserFlightRepository>();
builder.Services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();

builder.Services.AddSingleton<IClock, UtcClock>();
builder.Services.AddScoped<IAirportService, AirportService>();
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IMapFlightService, MapFlightService>();
builder.Services.AddScoped<IFlightStatsService, FlightStatsService>();
builder.Services.AddScoped<IPassportService, PassportService>();
builder.Services.AddScoped<IAirportOverviewService, AirportOverviewService>();
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
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
            "See doc/ApiVersioningPolicy.md";
    }

    await next();
});

app.MapGet("/api/versioning", () => Results.Ok(new
{
    currentVersion = "v1",
    supportedVersions = new[] { "v1" },
    deprecatedVersions = Array.Empty<string>(),
    policyDocument = "doc/ApiVersioningPolicy.md"
}));

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
