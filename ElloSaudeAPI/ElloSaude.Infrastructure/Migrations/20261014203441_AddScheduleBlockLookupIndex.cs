using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElloSaude.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleBlockLookupIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "ScheduleBlocks",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleBlocks_TenantId_ProfessionalId_IsActive_StartDateTime",
                table: "ScheduleBlocks",
                columns: new[] { "TenantId", "ProfessionalId", "IsActive", "StartDateTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScheduleBlocks_TenantId_ProfessionalId_IsActive_StartDateTime",
                table: "ScheduleBlocks");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "ScheduleBlocks",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
