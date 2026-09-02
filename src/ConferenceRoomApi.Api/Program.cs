using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using ConferenceRoomApi.Api.Common.ExceptionHandling;
using ConferenceRoomApi.Api.Common.Security;
using ConferenceRoomApi.Api.Common.Swagger;
using ConferenceRoomApi.Api.Common.Validation;
using ConferenceRoomApi.Application.AdditionalServices;
using ConferenceRoomApi.Application.Bookings;
using ConferenceRoomApi.Application.Rooms;
using ConferenceRoomApi.Infrastructure;
using ConferenceRoomApi.Infrastructure.Persistence;
using ConferenceRoomApi.Infrastructure.Seed;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ConferenceRoomApi.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console());

        // ---- Services ----------------------------------------------------

        builder.Services.AddControllers(options => options.Filters.Add<ValidationActionFilter>())
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.AddInfrastructure(builder.Configuration);

        // Application-layer use-case orchestrators: concrete classes, no interface needed —
        // they ARE the abstraction the API layer depends on. Repositories/clock/pricing
        // policy underneath are already interface-based (see AddInfrastructure).
        builder.Services.AddScoped<RoomsService>();
        builder.Services.AddScoped<BookingsService>();
        builder.Services.AddScoped<AdditionalServicesService>();

        builder.Services.AddValidatorsFromAssemblyContaining<RoomsService>();

        builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection(ApiKeyOptions.SectionName));

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddApiSwagger();

        builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();

        var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            if (corsOrigins.Length > 0)
            {
                policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod();
            }
            else if (builder.Environment.IsDevelopment())
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            }
        }));

        var permitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 100);
        var windowSeconds = builder.Configuration.GetValue("RateLimiting:WindowSeconds", 60);
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        QueueLimit = 0
                    }));
        });

        var app = builder.Build();

        // ---- Pipeline -------------------------------------------------------

        app.UseExceptionHandler();

        // Must run before anything that reads the client IP or scheme (the rate limiter and
        // HTTPS redirection below): without it, both see the reverse proxy's address/scheme
        // instead of the real client's whenever this API sits behind one. KnownProxies /
        // KnownNetworks are left at their defaults (trust only loopback) — a production
        // deployment behind a specific proxy should configure those explicitly rather than
        // trust every forwarded header blindly.
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseSerilogRequestLogging();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("Referrer-Policy", "no-referrer");
            await next();
        });

        // On by default so the assignment is easy to evaluate; a real production deployment
        // of a system with sensitive data would flip "EnableSwagger" to false in its config.
        if (app.Configuration.GetValue("EnableSwagger", true))
        {
            app.UseSwagger();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Conference Room Booking API v1"));
        }

        app.UseHttpsRedirection();
        app.UseCors();
        app.UseRateLimiter();
        app.UseMiddleware<ApiKeyMiddleware>();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health");

        await ApplyMigrationsAndSeedAsync(app);

        app.Run();
    }

    /// <summary>
    /// Applies pending EF Core migrations and loads the starter catalog on startup. Safe to
    /// run every time: migrations are idempotent and <see cref="DatabaseSeeder"/> only
    /// inserts when the additional-services table is empty.
    /// </summary>
    private static async Task ApplyMigrationsAndSeedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(db);
    }
}
