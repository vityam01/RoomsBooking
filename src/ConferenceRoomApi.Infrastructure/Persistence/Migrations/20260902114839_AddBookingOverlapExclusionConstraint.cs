using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConferenceRoomApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingOverlapExclusionConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Required so GiST can index the plain-equality RoomId column used in the
            // exclusion constraint below (GiST has no native support for "=" on uuid
            // without it).
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            // Last line of defense against double-booking a room: even if two requests race
            // past the application-level overlap check at the same instant, only one of the
            // resulting INSERTs can win here. tsrange(...) is built from booking_date +
            // start_time/end_time directly in the constraint expression, so no extra column
            // is needed. Cancelled bookings are excluded so cancelling one frees the slot.
            // Column names are quoted PascalCase because that's what EF Core's default
            // (non-snake_case) naming convention emitted them as in InitialCreate.
            migrationBuilder.Sql(@"
                ALTER TABLE bookings
                ADD CONSTRAINT ex_bookings_no_overlap
                EXCLUDE USING gist (
                    ""RoomId"" WITH =,
                    tsrange(""BookingDate"" + ""StartTime"", ""BookingDate"" + ""EndTime"") WITH &&
                )
                WHERE (""Status"" = 'Confirmed');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE bookings DROP CONSTRAINT ex_bookings_no_overlap;");
        }
    }
}
