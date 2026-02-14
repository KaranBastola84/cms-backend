using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cms_backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedCurrentStudentsField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentStudents",
                table: "Batches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentStudents",
                table: "Batches",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
