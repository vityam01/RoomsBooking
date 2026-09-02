using ConferenceRoomApi.Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceRoomApi.Infrastructure.Persistence.Configurations;

public sealed class RoomOfferingConfiguration : IEntityTypeConfiguration<RoomOffering>
{
    public void Configure(EntityTypeBuilder<RoomOffering> builder)
    {
        builder.ToTable("room_offerings");
        builder.HasKey(o => new { o.RoomId, o.AdditionalServiceId });

        builder.HasOne(o => o.AdditionalService)
            .WithMany()
            .HasForeignKey(o => o.AdditionalServiceId)
            .OnDelete(DeleteBehavior.Restrict); // a service in use by a room cannot vanish from under it
    }
}
