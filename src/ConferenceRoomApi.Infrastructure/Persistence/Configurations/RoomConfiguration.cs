using ConferenceRoomApi.Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceRoomApi.Infrastructure.Persistence.Configurations;

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("rooms");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Capacity).IsRequired();
        builder.Property(r => r.BasePricePerHour).HasPrecision(10, 2);
        builder.Property(r => r.IsActive).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();

        builder.HasIndex(r => r.IsActive);

        // Optimistic concurrency via Postgres's built-in xmin system column — no extra
        // column, no extra migration-managed schema. Two concurrent PUTs against the same
        // room now produce a 409 for the loser instead of a silent last-write-wins overwrite.
        builder.Property<uint>("xmin").IsRowVersion();

        builder.HasMany(r => r.Offerings)
            .WithOne()
            .HasForeignKey(o => o.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Offerings).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
