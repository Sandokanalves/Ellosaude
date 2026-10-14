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

    [Fact]
    public void Appointment_ShouldFollowReceptionAndServiceLifecycle()
    {
        var start = DateTime.UtcNow.AddHours(-1);
        var arrival = start.AddMinutes(10);
        var serviceStart = arrival.AddMinutes(15);
        var appointment = new Appointment(
            start,
            start.AddMinutes(30),
            Guid.NewGuid(),
            Guid.NewGuid(),
            AppointmentType.Consulta,
            "tenant-a");

        appointment.Confirm();
        appointment.UpdateStatus(AppointmentStatus.AguardandoRecepcao, arrival);
        appointment.UpdateStatus(AppointmentStatus.EmAtendimento, serviceStart);
        appointment.UpdateStatus(AppointmentStatus.Realizado, serviceStart.AddMinutes(20));

        appointment.Status.Should().Be(AppointmentStatus.Realizado);
        appointment.ArrivedAt.Should().Be(arrival);
        appointment.ServiceStartedAt.Should().Be(serviceStart);
    }

    [Fact]
    public void Appointment_ShouldRejectSkippingReceptionBeforeStartingService()
    {
        var appointment = CreateAppointment(DateTime.UtcNow.AddHours(1));
        appointment.Confirm();

        var act = () => appointment.UpdateStatus(AppointmentStatus.EmAtendimento);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*aguardando na recepção*");
    }

    [Fact]
    public void Appointment_ShouldRejectNoShowBeforeScheduledStart()
    {
        var appointment = CreateAppointment(DateTime.UtcNow.AddHours(1));
        appointment.Confirm();

        var act = () => appointment.UpdateStatus(AppointmentStatus.Faltou, DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*após o horário previsto*");
    }

    [Fact]
    public void Appointment_ShouldAllowNoShowAfterScheduledStart()
    {
        var start = DateTime.UtcNow.AddHours(-1);
        var appointment = CreateAppointment(start);
        appointment.Confirm();

        appointment.UpdateStatus(AppointmentStatus.Faltou, start.AddMinutes(10));

        appointment.Status.Should().Be(AppointmentStatus.Faltou);
    }

    private static Appointment CreateAppointment(DateTime start) =>
        new(
            start,
            start.AddMinutes(30),
            Guid.NewGuid(),
            Guid.NewGuid(),
            AppointmentType.Consulta,
            "tenant-a");
}
