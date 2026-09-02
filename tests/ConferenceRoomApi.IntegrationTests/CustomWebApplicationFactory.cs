using ConferenceRoomApi.Infrastructure.Persistence;
using ConferenceRoomApi.Infrastructure.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConferenceRoomApi.IntegrationTests;

/// <summary>
/// Boots the real Api host against a dedicated Postgres database (conference_rooms_test),
/// not an in-memory provider — the whole point of these tests is to exercise the actual
/// EF Core mappings and the exclusion-constraint-backed conflict path, which an in-memory
/// provider can't reproduce.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Api.Program>
{
    public static readonly string TestConnectionString =
        Environment.GetEnvironmentVariable("CONFERENCE_ROOM_API_TEST_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=conference_rooms_test;Username=postgres;Password=postgres";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = TestConnectionString
            });
        });
    }

    /// <summary>
    /// Clears everything a test creates (rooms, bookings and their offshoots) but
    /// deliberately leaves additional_services alone: the catalog is seeded once when the
    /// shared host first boots, and tests are expected to look services up rather than
    /// recreate them, exactly like a real client would against a long-running API.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(db);
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE booked_price_segments, booked_services, bookings, room_offerings, rooms RESTART IDENTITY CASCADE;");
    }
}
