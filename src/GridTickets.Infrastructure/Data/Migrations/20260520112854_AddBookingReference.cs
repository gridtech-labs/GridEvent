using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridTickets.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookingReference",
                table: "orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_BookingReference",
                table: "orders",
                column: "BookingReference",
                unique: true,
                filter: "\"BookingReference\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_BookingReference",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "BookingReference",
                table: "orders");
        }
    }
}
