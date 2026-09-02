using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConferenceRoomApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimisticConcurrencyToRoomAndAdditionalService : Migration
    {
        // Intentionally a no-op migration. "xmin" is a PostgreSQL system column that already
        // exists on every table — it cannot be added (Postgres rejects a column named
        // "xmin") and doesn't need to be. This migration exists only so the EF Core model
        // snapshot picks up the new IsRowVersion() shadow property; there is no real schema
        // change to apply. See RoomConfiguration/AdditionalServiceConfiguration.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
