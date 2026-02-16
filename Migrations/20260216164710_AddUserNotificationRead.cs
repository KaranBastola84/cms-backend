using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace cms_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNotificationRead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserNotificationReads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RelatedId = table.Column<int>(type: "integer", nullable: false),
                    NotificationKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationReads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotificationReads_ApplicationUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationReads_NotificationKey",
                table: "UserNotificationReads",
                column: "NotificationKey");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationReads_UserId",
                table: "UserNotificationReads",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationReads_UserId_NotificationKey",
                table: "UserNotificationReads",
                columns: new[] { "UserId", "NotificationKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserNotificationReads");
        }
    }
}
