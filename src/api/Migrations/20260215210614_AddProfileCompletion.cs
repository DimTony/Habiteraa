using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habitera.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ProfileCompleted",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProfileCompletedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileCompleted",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileCompletedAt",
                table: "AspNetUsers");
        }
    }
}
