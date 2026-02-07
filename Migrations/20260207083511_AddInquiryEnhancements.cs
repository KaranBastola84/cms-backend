using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace cms_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddInquiryEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "Inquiries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedToId",
                table: "Inquiries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConvertedAt",
                table: "Inquiries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConvertedToStudentId",
                table: "Inquiries",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FollowUpNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InquiryId = table.Column<int>(type: "integer", nullable: false),
                    CreatedById = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUpNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FollowUpNotes_ApplicationUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FollowUpNotes_Inquiries_InquiryId",
                        column: x => x.InquiryId,
                        principalTable: "Inquiries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inquiries_AssignedToId",
                table: "Inquiries",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpNotes_CreatedById",
                table: "FollowUpNotes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpNotes_InquiryId",
                table: "FollowUpNotes",
                column: "InquiryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inquiries_ApplicationUsers_AssignedToId",
                table: "Inquiries",
                column: "AssignedToId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inquiries_ApplicationUsers_AssignedToId",
                table: "Inquiries");

            migrationBuilder.DropTable(
                name: "FollowUpNotes");

            migrationBuilder.DropIndex(
                name: "IX_Inquiries_AssignedToId",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "AssignedToId",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "ConvertedAt",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "ConvertedToStudentId",
                table: "Inquiries");
        }
    }
}
