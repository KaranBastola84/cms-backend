using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cms_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainerToBatchAndCapacityValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrainerId",
                table: "Batches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Batches_TrainerId",
                table: "Batches",
                column: "TrainerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_ApplicationUsers_TrainerId",
                table: "Batches",
                column: "TrainerId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Batches_ApplicationUsers_TrainerId",
                table: "Batches");

            migrationBuilder.DropIndex(
                name: "IX_Batches_TrainerId",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "TrainerId",
                table: "Batches");
        }
    }
}
