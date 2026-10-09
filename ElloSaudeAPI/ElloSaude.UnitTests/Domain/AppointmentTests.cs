using ElloSaude.Domain.Entities;
using ElloSaude.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ElloSaude.UnitTests.Domain;

public class AppointmentTests
{
    [Fact]
    public void Appointment_Creation_ShouldSetDefaultStatusPendente()
    {
        // Arrange
        var start = DateTime.UtcNow.AddDays(1);
        var end = start.AddHours(1);
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var tenantId = "tenant-abc";

        // Act
        var appointment = new Appointment(start, end, patientId, doctorId, AppointmentType.Consulta, tenantId);

        // Assert
        appointment.Id.Should().NotBeEmpty();
        appointment.Status.Should().Be(AppointmentStatus.Pendente);
        appointment.PatientId.Should().Be(patientId);
        appointment.ProfessionalId.Should().Be(doctorId);
        appointment.TenantId.Should().Be(tenantId);
    }
}
