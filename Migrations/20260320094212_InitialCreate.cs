using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace timer_web_app.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Timers",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TimerId = table.Column<string>(type: "text", nullable: true),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsFinished = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timers", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Timers_EndsAt",
                table: "Timers",
                column: "EndsAt");

            migrationBuilder.CreateIndex(
                name: "IX_Timers_UserId",
                table: "Timers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Timers_UserId_IsFinished",
                table: "Timers",
                columns: new[] { "UserId", "IsFinished" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Timers");
        }
    }
}
