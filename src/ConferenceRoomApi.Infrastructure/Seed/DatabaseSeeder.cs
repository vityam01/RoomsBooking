using ConferenceRoomApi.Domain.AdditionalServices;
using ConferenceRoomApi.Domain.Rooms;
using ConferenceRoomApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomApi.Infrastructure.Seed;

/// <summary>
/// Loads the starting catalog described in the assignment (rooms A/B/C, the three
/// additional services) so the API is usable immediately after `docker compose up`.
/// Idempotent: does nothing if any additional service already exists.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.AdditionalServices.AnyAsync(cancellationToken))
        {
            return;
        }

        var projector = AdditionalService.Create("Проєктор", 500m);
        var wifi = AdditionalService.Create("Wi-Fi", 300m);
        var sound = AdditionalService.Create("Звук", 700m);
        db.AdditionalServices.AddRange(projector, wifi, sound);

        var roomA = Room.Create("Зал А", 50, 2000m);
        roomA.ReplaceOfferings(new[] { projector.Id, wifi.Id });

        var roomB = Room.Create("Зал B", 100, 3500m);
        roomB.ReplaceOfferings(new[] { projector.Id, wifi.Id, sound.Id });

        var roomC = Room.Create("Зал C", 30, 1500m);
        roomC.ReplaceOfferings(new[] { wifi.Id });

        db.Rooms.AddRange(roomA, roomB, roomC);

        await db.SaveChangesAsync(cancellationToken);
    }
}
