using ConferenceRoomApi.Domain.Bookings;
using ConferenceRoomApi.Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceRoomApi.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.RoomId).IsRequired();
        builder.Property(b => b.BookingDate).IsRequired().HasColumnType("date");
        builder.Property(b => b.StartTime).IsRequired().HasColumnType("time");
        builder.Property(b => b.EndTime).IsRequired().HasColumnType("time");
        builder.Property(b => b.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.RoomCost).HasPrecision(10, 2);
        builder.Property(b => b.ServicesCost).HasPrecision(10, 2);
        builder.Property(b => b.TotalCost).HasPrecision(10, 2);
        builder.Property(b => b.CreatedAt).IsRequired();

        // No navigation property to Room on purpose (Booking only needs the id); the FK is
        // still declared so the database enforces referential integrity.
        builder.HasOne<Room>().WithMany().HasForeignKey(b => b.RoomId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new { b.RoomId, b.BookingDate });
        builder.HasIndex(b => b.Status);

        builder.HasMany(b => b.BookedServices)
            .WithOne()
            .HasForeignKey(s => s.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(b => b.BookedServices).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(b => b.PriceSegments)
            .WithOne()
            .HasForeignKey(s => s.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(b => b.PriceSegments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class BookedServiceConfiguration : IEntityTypeConfiguration<BookedService>
{
    public void Configure(EntityTypeBuilder<BookedService> builder)
    {
        builder.ToTable("booked_services");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Price).HasPrecision(10, 2);
    }
}

public sealed class BookedPriceSegmentConfiguration : IEntityTypeConfiguration<BookedPriceSegment>
{
    public void Configure(EntityTypeBuilder<BookedPriceSegment> builder)
    {
        builder.ToTable("booked_price_segments");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Zone).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Start).IsRequired().HasColumnType("time");
        builder.Property(s => s.End).IsRequired().HasColumnType("time");
        builder.Property(s => s.Hours).HasPrecision(5, 2);
        builder.Property(s => s.Multiplier).HasPrecision(4, 2);
        builder.Property(s => s.RatePerHour).HasPrecision(10, 2);
        builder.Property(s => s.SegmentCost).HasPrecision(10, 2);
    }
}
