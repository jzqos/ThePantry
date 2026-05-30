using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThePantry.Migrations
{
    /// <inheritdoc />
    public partial class AddLabelQueueItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LabelQueueItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Upc = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResultName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ResultSpecies = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ResultWeight = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LinkedInventoryItemId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelQueueItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabelQueueItems_InventoryItems_LinkedInventoryItemId",
                        column: x => x.LinkedInventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabelQueueItems_Status",
                table: "LabelQueueItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LabelQueueItems_Timestamp",
                table: "LabelQueueItems",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_LabelQueueItems_LinkedInventoryItemId",
                table: "LabelQueueItems",
                column: "LinkedInventoryItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LabelQueueItems");
        }
    }
}
