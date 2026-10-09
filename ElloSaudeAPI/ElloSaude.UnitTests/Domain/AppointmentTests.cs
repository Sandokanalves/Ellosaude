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

    [Fact]
    public void MarkAsFreeReturn_ShouldZeroPriceForReturnAppointment()
    {
        var appointment = new Appointment(
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Guid.NewGuid(),
            Guid.NewGuid(),
            AppointmentType.Retorno,
            "tenant-a",
            price: 150);

        appointment.MarkAsFreeReturn();

        appointment.IsFreeReturn.Should().BeTrue();
        appointment.Price.Should().Be(0);
    }

    [Fact]
    public void MarkAsFreeReturn_ShouldRejectNonReturnAppointment()
    {
        var appointment = new Appointment(
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Guid.NewGuid(),
            Guid.NewGuid(),
            AppointmentType.Consulta,
            "tenant-a",
            price: 150);

        var act = () => appointment.MarkAsFreeReturn();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*agendamento de retorno*");
    }
}
