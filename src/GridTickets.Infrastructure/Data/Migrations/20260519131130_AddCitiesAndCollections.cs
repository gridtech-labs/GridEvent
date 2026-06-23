using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridTickets.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCitiesAndCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CollectionId",
                table: "events",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "collections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_events_CollectionId",
                table: "events",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_cities_IsActive",
                table: "cities",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_cities_IsDeleted_SortOrder",
                table: "cities",
                columns: new[] { "IsDeleted", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_collections_IsActive",
                table: "collections",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_collections_IsDeleted_SortOrder",
                table: "collections",
                columns: new[] { "IsDeleted", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_events_collections_CollectionId",
                table: "events",
                column: "CollectionId",
                principalTable: "collections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_events_collections_CollectionId",
                table: "events");

            migrationBuilder.DropTable(
                name: "collections");

            migrationBuilder.DropTable(
                name: "cities");

            migrationBuilder.DropIndex(
                name: "IX_events_CollectionId",
                table: "events");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                table: "events");
        }
    }
}
