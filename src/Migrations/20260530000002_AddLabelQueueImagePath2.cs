using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThePantry.Migrations
{
    /// <inheritdoc />
    public partial class AddLabelQueueImagePath2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath2",
                table: "LabelQueueItems",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath2",
                table: "LabelQueueItems");
        }
    }
}
