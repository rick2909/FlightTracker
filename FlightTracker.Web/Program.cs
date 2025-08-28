using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Application.Services.Interfaces.Analytics;
using FlightTracker.Application.Services.Implementation.Analytics;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Infrastructure.Repositories.Implementation;
using FlightTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Radzen;
using FlightTracker.Application.Mapping;
using FlightTracker.Infrastructure.External;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
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

// Register application services
builder.Services.AddScoped<IAirportService, AirportService>();
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IUserFlightService, UserFlightService>();
builder.Services.AddScoped<IMapFlightService, MapFlightService>();
builder.Services.AddScoped<IPassportService, PassportService>();
builder.Services.AddScoped<IAirportOverviewService, AirportOverviewService>();
builder.Services.AddHttpClient<ITimeApiService, TimeApiService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(3);
});
builder.Services.AddScoped<IFlightLookupService, FlightLookupService>();
builder.Services.AddHttpClient<IAirportLiveService, FlightTracker.Infrastructure.External.AviationstackService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(6);
});

// ADSBdb route lookup and metadata provisioner
builder.Services.AddHttpClient<IFlightRouteLookupClient, AdsBdbClient>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(6);
});
builder.Services.AddScoped<IFlightMetadataProvisionService, FlightMetadataProvisionService>();

// External provider(s)
builder.Services.AddScoped<IFlightDataProvider, FlightTracker.Infrastructure.External.OpenSkyClient>();

// AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<ApplicationMappingProfile>();
});

// Identity (basic, for seeding users)
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<FlightTrackerDbContext>();

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

app.UseAuthorization();

app.MapStaticAssets();

// Set Dashboard as the default route instead of Home
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapBlazorHub();

app.Run();