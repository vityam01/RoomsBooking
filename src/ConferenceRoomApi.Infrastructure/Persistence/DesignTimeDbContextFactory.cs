using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ConferenceRoomApi.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add/update` construct an ApplicationDbContext without running
/// the whole Api host. The connection string only needs to be valid enough for EF to talk
/// to Postgres about schema — it is never used at application runtime (Program.cs wires the
/// real one from configuration).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONFERENCE_ROOM_API_DESIGN_TIME_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=conference_rooms_dev;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
