using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElloSaude.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientPortalOwnershipAndAppointmentBoundRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Patients",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLegacyUnlinked",
                table: "MedicalRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_AppointmentId",
                table: "PaymentRecords",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_PatientId",
                table: "PaymentRecords",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_ProfessionalId",
                table: "PaymentRecords",
                column: "ProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserId",
                table: "Patients",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_AppointmentId",
                table: "MedicalRecords",
                column: "AppointmentId",
                unique: true,
                filter: "[AppointmentId] IS NOT NULL");

            migrationBuilder.Sql(
                "UPDATE [MedicalRecords] SET [IsLegacyUnlinked] = 1 WHERE [AppointmentId] IS NULL;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MedicalRecords_AppointmentOrLegacy",
                table: "MedicalRecords",
                sql: "[AppointmentId] IS NOT NULL OR [IsLegacyUnlinked] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalRecords_Appointments_AppointmentId",
                table: "MedicalRecords",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Users_UserId",
                table: "Patients",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRecords_Appointments_AppointmentId",
                table: "PaymentRecords",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRecords_Patients_PatientId",
                table: "PaymentRecords",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRecords_Professionals_ProfessionalId",
                table: "PaymentRecords",
                column: "ProfessionalId",
                principalTable: "Professionals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalRecords_Appointments_AppointmentId",
                table: "MedicalRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Users_UserId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRecords_Appointments_AppointmentId",
                table: "PaymentRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRecords_Patients_PatientId",
                table: "PaymentRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRecords_Professionals_ProfessionalId",
                table: "PaymentRecords");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRecords_AppointmentId",
                table: "PaymentRecords");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRecords_PatientId",
                table: "PaymentRecords");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRecords_ProfessionalId",
                table: "PaymentRecords");

            migrationBuilder.DropIndex(
                name: "IX_Patients_UserId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_MedicalRecords_AppointmentId",
                table: "MedicalRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MedicalRecords_AppointmentOrLegacy",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "IsLegacyUnlinked",
                table: "MedicalRecords");
        }
    }
}
