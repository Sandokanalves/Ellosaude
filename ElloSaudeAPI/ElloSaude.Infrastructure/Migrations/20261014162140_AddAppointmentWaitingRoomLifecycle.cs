using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElloSaude.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentWaitingRoomLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArrivedAt",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServiceStartedAt",
                table: "Appointments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArrivedAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ServiceStartedAt",
                table: "Appointments");
        }
    }
}
