using System.Reflection;
using ConferenceRoomApi.Application.Common.Interfaces;
using ConferenceRoomApi.Domain.AdditionalServices;
using ConferenceRoomApi.Domain.Bookings;
using ConferenceRoomApi.Domain.Rooms;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomApi.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IUnitOfWork
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<AdditionalService> AdditionalServices => Set<AdditionalService>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    // Explicit implementation: DbContext already exposes a public SaveChangesAsync that
    // returns Task<int>, so IUnitOfWork's Task-returning member has to be implemented
    // explicitly to avoid a signature clash.
    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
        => await SaveChangesAsync(cancellationToken);
}
