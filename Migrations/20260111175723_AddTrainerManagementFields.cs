using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cms_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainerManagementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrainerRole",
                table: "ApplicationUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrainerRole",
                table: "ApplicationUsers");
        }
    }
}
