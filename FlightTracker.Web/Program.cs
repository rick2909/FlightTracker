using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Application.Services.Implementation;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Infrastructure.Repositories.Implementation;
using FlightTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Radzen;
using FlightTracker.Web;

var builder = WebApplication.CreateBuilder(args);

// Add MVC (Radzen/Blazor deferred until components added)
builder.Services.AddControllersWithViews();
// Configure known HTTPS port for HttpsRedirectionMiddleware
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 7222; // matches launchSettings https profile
});

// Configure Entity Framework
builder.Services.AddDbContext<FlightTrackerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories
builder.Services.AddScoped<IAirportRepository, AirportRepository>();
builder.Services.AddScoped<IFlightRepository, FlightRepository>();
builder.Services.AddScoped<IUserFlightRepository, UserFlightRepository>();

// Register application services
builder.Services.AddScoped<IAirportService, AirportService>();
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IUserFlightService, UserFlightService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
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