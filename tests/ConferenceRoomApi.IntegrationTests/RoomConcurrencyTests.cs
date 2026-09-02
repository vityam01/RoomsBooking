using ConferenceRoomApi.Domain.Rooms;
using ConferenceRoomApi.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ConferenceRoomApi.IntegrationTests;

/// <summary>
/// Verifies the "xmin"-based optimistic concurrency token configured on Room (see
/// RoomConfiguration.cs) actually stops a lost update. Exercised directly against two
/// independent DbContext scopes — the same shape two truly concurrent HTTP requests would
/// produce — rather than through the HTTP API, since RoomDto doesn't (yet) round-trip a
/// version token for a client to send back; today's protection covers two requests racing
/// within the server, not a client editing a long-stale copy.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class RoomConcurrencyTests : IntegrationTestBase
{
    public RoomConcurrencyTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ConcurrentUpdates_ToTheSameRoom_SecondSaveThrowsConcurrencyException()
    {
        Guid roomId;
        using (var seedScope = Factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var room = Room.Create("Зал Конкуренція", 10, 1000m);
            seedDb.Rooms.Add(room);
            await seedDb.SaveChangesAsync();
            roomId = room.Id;
        }

        using var scopeA = Factory.Services.CreateScope();
        using var scopeB = Factory.Services.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var roomA = await dbA.Rooms.FirstAsync(r => r.Id == roomId);
        var roomB = await dbB.Rooms.FirstAsync(r => r.Id == roomId);

        roomA.UpdateDetails("Зал Конкуренція (A)", 10, 1000m);
        await dbA.SaveChangesAsync(); // wins the race

        roomB.UpdateDetails("Зал Конкуренція (B)", 10, 1000m);
        var act = async () => await dbB.SaveChangesAsync(); // loses — its xmin is now stale

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}
