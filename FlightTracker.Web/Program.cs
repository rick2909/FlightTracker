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
using FlightTracker.Web;

var builder = WebApplication.CreateBuilder(args);

// Add MVC (Radzen/Blazor deferred until components added)
builder.Services.AddControllersWithViews();

// Configure Entity Framework (SQLite for local inspection)
builder.Services.AddDbContext<FlightTrackerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=flighttracker.dev.db"));

// Register repositories
builder.Services.AddScoped<IAirportRepository, AirportRepository>();
builder.Services.AddScoped<IFlightRepository, FlightRepository>();
builder.Services.AddScoped<IUserFlightRepository, UserFlightRepository>();

// Register application services
builder.Services.AddScoped<IAirportService, AirportService>();
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IUserFlightService, UserFlightService>();
builder.Services.AddScoped<IMapFlightService, MapFlightService>();

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

// Always redirect to HTTPS in dev & prod (after ensuring dev cert trusted)
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

// (Radzen/Blazor root removed – no interactive components yet)

app.Run();