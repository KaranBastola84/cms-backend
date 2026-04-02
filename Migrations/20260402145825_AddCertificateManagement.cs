using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace cms_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Certificates",
                columns: table => new
                {
                    CertificateId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CertificateNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    ModuleName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TrainerReportedProgressPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AttendancePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    IsPaymentCleared = table.Column<bool>(type: "boolean", nullable: false),
                    RecommendationNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RecommendedByTrainerId = table.Column<int>(type: "integer", nullable: false),
                    RecommendedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IssuedByAdminId = table.Column<int>(type: "integer", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DeliveryMode = table.Column<int>(type: "integer", nullable: false),
                    AdminNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByAdminId = table.Column<int>(type: "integer", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.CertificateId);
                    table.ForeignKey(
                        name: "FK_Certificates_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CertificateNumber",
                table: "Certificates",
                column: "CertificateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Status",
                table: "Certificates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_StudentId",
                table: "Certificates",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_VerificationToken",
                table: "Certificates",
                column: "VerificationToken",
                unique: true);

            migrationBuilder.Sql(
                "INSERT INTO \"RolePermissions\" (\"Role\", \"PermissionKey\") " +
                "SELECT 'Trainer', 'certificates' " +
                "WHERE NOT EXISTS (" +
                "SELECT 1 FROM \"RolePermissions\" WHERE \"Role\" = 'Trainer' AND \"PermissionKey\" = 'certificates');");

            migrationBuilder.Sql(
                "INSERT INTO \"RolePermissions\" (\"Role\", \"PermissionKey\") " +
                "SELECT 'Student', 'certificates' " +
                "WHERE NOT EXISTS (" +
                "SELECT 1 FROM \"RolePermissions\" WHERE \"Role\" = 'Student' AND \"PermissionKey\" = 'certificates');");

            migrationBuilder.Sql(
                "INSERT INTO \"RolePermissions\" (\"Role\", \"PermissionKey\") " +
                "SELECT 'EnrolledStudent', 'certificates' " +
                "WHERE NOT EXISTS (" +
                "SELECT 1 FROM \"RolePermissions\" WHERE \"Role\" = 'EnrolledStudent' AND \"PermissionKey\" = 'certificates');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"RolePermissions\" WHERE \"Role\" IN ('Trainer', 'Student', 'EnrolledStudent') AND \"PermissionKey\" = 'certificates';");

            migrationBuilder.DropTable(
                name: "Certificates");
        }
    }
}
