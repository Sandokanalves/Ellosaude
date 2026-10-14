using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElloSaude.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: false),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedByRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentStatusHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentStatusHistory_TenantId_AppointmentId_OccurredAt",
                table: "AppointmentStatusHistory",
                columns: new[] { "TenantId", "AppointmentId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentStatusHistory");
        }
    }
}
