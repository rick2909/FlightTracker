using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Application.Services.Interfaces.Analytics;
using FlightTracker.Application.Services.Implementation.Analytics;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Infrastructure.Repositories.Implementation;
using FlightTracker.Infrastructure.Data;
using FlightTracker.Web.Models.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Radzen;
using FlightTracker.Application.Mapping;
using FlightTracker.Infrastructure.External;
using Polly;
using Polly.Contrib.WaitAndRetry;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection(AuthSettings.SectionName));
builder.Services.AddHttpContextAccessor();
builder.Services.AddServerSideBlazor().AddCircuitOptions(o =>
{
    if (builder.Environment.IsDevelopment())
    {
        o.DetailedErrors = true;
    }
});

// Radzen services (Notification, Dialog, Tooltip, ContextMenu)
builder.Services.AddRadzenComponents();

// Configure Entity Framework (SQLite for local inspection)
builder.Services.AddDbContext<FlightTrackerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=flighttracker.dev.db"));

// Register repositories
builder.Services.AddScoped<IAirportRepository, AirportRepository>();
builder.Services.AddScoped<IFlightRepository, FlightRepository>();
builder.Services.AddScoped<IUserFlightRepository, UserFlightRepository>();
builder.Services.AddScoped<IAircraftRepository, AircraftRepository>();
builder.Services.AddScoped<IAirlineRepository, AirlineRepository>();
builder.Services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();

// Register application services
builder.Services.AddScoped<IAirportService, AirportService>();
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IUserFlightService, UserFlightService>();
builder.Services.AddScoped<IMapFlightService, MapFlightService>();
builder.Services.AddScoped<IPassportService, PassportService>();
builder.Services.AddScoped<IAirportOverviewService, AirportOverviewService>();
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
builder.Services.AddHttpClient<ITimeApiService, TimeApiService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(3);
})
.AddPolicyHandler(
    Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .OrResult(r => (int)r.StatusCode is >= 500 or 429)
        .WaitAndRetryAsync(
            Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(100), 2),
            onRetry: (outcome, delay, attempt, ctx) => { }
        )
);
builder.Services.AddScoped<IFlightLookupService, FlightLookupService>();
builder.Services.AddHttpClient<IAirportLiveService, AviationstackService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(6);
})
// Basic transient fault-handling: retry a few times with exponential backoff + jitter
.AddPolicyHandler(
    Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .OrResult(r => (int)r.StatusCode is >= 500 or 429)
        .WaitAndRetryAsync(
            Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(200), 3),
            onRetry: (outcome, delay, attempt, ctx) =>
            {
                // no logging in Presentation per guidelines; rely on provider/internal logs if needed
            }
        )
);

// ADSBdb route lookup and metadata provisioner
builder.Services.AddHttpClient<AdsBdbClient>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(6);
});
builder.Services.AddScoped<IFlightRouteLookupClient>(sp => sp.GetRequiredService<AdsBdbClient>());
builder.Services.AddScoped<IAircraftLookupClient>(sp => sp.GetRequiredService<AdsBdbClient>());
builder.Services.AddScoped<IFlightMetadataProvisionService, FlightMetadataProvisionService>();

// AirportDB client for airport enrichment
builder.Services.AddHttpClient<AirportDbClient>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(6);
});
builder.Services.AddScoped<IAirportLookupClient>(sp => sp.GetRequiredService<AirportDbClient>());
builder.Services.AddScoped<IAirportEnrichmentService, AirportEnrichmentService>();

// External provider(s)
builder.Services.AddScoped<IFlightDataProvider, OpenSkyClient>();

// Aircraft photo service for airport-data.com API
builder.Services.AddHttpClient<IAircraftPhotoService, AircraftPhotoService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(5);
})
.AddPolicyHandler(
    Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .OrResult(r => (int)r.StatusCode is >= 500 or 429)
        .WaitAndRetryAsync(
            Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(100), 2),
            onRetry: (outcome, delay, attempt, ctx) => { }
        )
);

// AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<ApplicationMappingProfile>();
    cfg.AddProfile<FlightProfile>();
    cfg.AddProfile<FlightTracker.Web.Mapping.WebMappingProfile>();
});

// Identity (basic, for seeding users)
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        
        // Configure password policy
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<FlightTrackerDbContext>()
    .AddSignInManager();

builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorization();

// Application services
builder.Services.AddScoped<IUsernameValidationService, UsernameValidationService>();

// Analytics services
builder.Services.AddSingleton<IDistanceCalculator, DistanceCalculator>(); // stateless, safe as singleton
builder.Services.AddSingleton<IEmissionCalculator, SimpleEmissionCalculator>(); // stateless
builder.Services.AddScoped<IFlightAnalyticsService, FlightAnalyticsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection();

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

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Route authenticated users to Dashboard, unauthenticated to Home
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapBlazorHub();

if (app.Environment.IsDevelopment())
{
    _ = app.MapPost("/dev-login", async (
        HttpContext httpContext,
        IOptions<AuthSettings> options) =>
    {
        var settings = options.Value;
        if (!settings.EnableDevBypass)
        {
            return Results.NotFound();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, settings.DevUserId.ToString()),
            new(ClaimTypes.Name, settings.DevUserName ?? string.Empty),
            new(ClaimTypes.Email, settings.DevEmail ?? string.Empty),
            new("display_name", settings.DevDisplayName ?? string.Empty)
        };

        var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal);

        var returnUrl = httpContext.Request.Query["ReturnUrl"].ToString();
        if (!string.IsNullOrWhiteSpace(returnUrl)
            && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
            && returnUrl.StartsWith("/", StringComparison.Ordinal)
            && !returnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return Results.Redirect(returnUrl);
        }

        return Results.Redirect("/Dashboard");
    });
}

app.Run();