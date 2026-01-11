using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cms_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffManagementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "ApplicationUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "ApplicationUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "ApplicationUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OTP",
                table: "ApplicationUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OTPExpiry",
                table: "ApplicationUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "ApplicationUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffRole",
                table: "ApplicationUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "OTP",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "OTPExpiry",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "StaffRole",
                table: "ApplicationUsers");
        }
    }
}
