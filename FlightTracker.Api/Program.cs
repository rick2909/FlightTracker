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
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Contrib.WaitAndRetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
app.MapControllers();

app.Run();
