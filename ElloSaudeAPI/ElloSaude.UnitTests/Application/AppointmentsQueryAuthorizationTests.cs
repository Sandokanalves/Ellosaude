using AutoMapper;
using ElloSaude.Application.Appointments.Queries;
using ElloSaude.Application.Common.Exceptions;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using ElloSaude.Domain.Enums;
using ElloSaude.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ElloSaude.UnitTests.Application;

public class AppointmentsQueryAuthorizationTests
{
    [Fact]
    public async Task GetAllAppointments_ShouldReturnOnlyAppointmentsForCurrentProfessional()
    {
        await using var fixture = await CreateFixture();
        var handler = fixture.CreateHandler("Profissional", fixture.UserAId);

        var results = await handler.Handle(new GetAllAppointmentsQuery(), CancellationToken.None);

        results.Should().ContainSingle();
        results[0].Id.Should().Be(fixture.AppointmentA.Id);
        results[0].ProfessionalName.Should().Be("Profissional A");
    }

    [Fact]
    public async Task GetAgendaByProfessional_ShouldNotReturnAnotherProfessionalsAgenda()
    {
        await using var fixture = await CreateFixture();
        var handler = fixture.CreateHandler("Profissional", fixture.UserAId);

        var act = () => handler.Handle(
            new GetAgendaByProfessionalQuery(fixture.ProfessionalB.Id, fixture.AppointmentA.StartTime),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Agenda não encontrada.");
    }

    [Fact]
    public async Task GetAppointmentById_ShouldHideAppointmentsOwnedByAnotherProfessional()
    {
        await using var fixture = await CreateFixture();
        var handler = fixture.CreateHandler("Profissional", fixture.UserAId);

        var act = () => handler.Handle(
            new GetAppointmentByIdQuery(fixture.AppointmentB.Id),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Agendamento não encontrado.");
    }

    [Fact]
    public async Task GetAppointmentById_ShouldReturnOwnAppointmentAndProfessionalName()
    {
        await using var fixture = await CreateFixture();
        var handler = fixture.CreateHandler("Profissional", fixture.UserAId);

        var result = await handler.Handle(
            new GetAppointmentByIdQuery(fixture.AppointmentA.Id),
            CancellationToken.None);

        result.Id.Should().Be(fixture.AppointmentA.Id);
        result.ProfessionalName.Should().Be("Profissional A");
    }

    [Fact]
    public async Task GetWaitingRoom_ShouldReturnOnlyCurrentProfessionalsArrivalsInArrivalOrder()
    {
        await using var fixture = await CreateFixture();
        var arrival = DateTime.UtcNow.AddMinutes(-20);
        fixture.AppointmentA.Confirm();
        fixture.AppointmentA.MarkAsWaiting(arrival);
        await fixture.Context.SaveChangesAsync();

        var handler = fixture.CreateHandler("Profissional", fixture.UserAId);
        var results = await handler.Handle(new GetWaitingRoomQuery(), CancellationToken.None);

        results.Should().ContainSingle();
        results[0].Id.Should().Be(fixture.AppointmentA.Id);
        results[0].ArrivedAt.Should().Be(arrival);
    }

    [Fact]
    public async Task GetWhatsAppConfirmationLink_ShouldUseLocalTimeAndNormalizedPatientPhone()
    {
        await using var fixture = await CreateFixture();
        var handler = fixture.CreateWhatsAppHandler("Secretaria", Guid.Empty);

        var result = await handler.Handle(
            new GetAppointmentWhatsAppLinkQuery(fixture.AppointmentA.Id, "America/Sao_Paulo"),
            CancellationToken.None);

        var link = new Uri(result.Url);
        link.Host.Should().Be("wa.me");
        link.AbsolutePath.Should().Be("/5511999998888");
        Uri.UnescapeDataString(link.Query["?text=".Length..])
            .Should().Contain("15/10/2026 às 12:00")
            .And.Contain("Para confirmar, responda 1")
            .And.Contain("cancelamento ou reagendamento");
    }

    [Fact]
    public async Task GetWhatsAppConfirmationLink_ShouldHideAppointmentsOwnedByAnotherProfessional()
    {
        await using var fixture = await CreateFixture();
        var handler = fixture.CreateWhatsAppHandler("Profissional", fixture.UserAId);

        var act = () => handler.Handle(
            new GetAppointmentWhatsAppLinkQuery(fixture.AppointmentB.Id, "UTC"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Agendamento não encontrado.");
    }

    [Fact]
    public async Task GetWhatsAppConfirmationLink_ShouldRejectAppointmentsAlreadyInProgress()
    {
        await using var fixture = await CreateFixture();
        fixture.AppointmentA.Confirm();
        fixture.AppointmentA.MarkAsWaiting();
        var handler = fixture.CreateWhatsAppHandler("Secretaria", Guid.Empty);

        var act = () => handler.Handle(
            new GetAppointmentWhatsAppLinkQuery(fixture.AppointmentA.Id, "UTC"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("O link de confirmação só pode ser gerado para agendamentos pendentes ou confirmados.");
    }

    [Fact]
    public async Task GetWhatsAppConfirmationLink_ShouldRejectUnknownTimeZonesAsValidationErrors()
    {
        await using var fixture = await CreateFixture();
        var handler = fixture.CreateWhatsAppHandler("Secretaria", Guid.Empty);

        var act = () => handler.Handle(
            new GetAppointmentWhatsAppLinkQuery(fixture.AppointmentA.Id, "Invalid/TimeZone"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("12345")]
    public async Task GetWhatsAppConfirmationLink_ShouldRejectMissingOrInvalidPatientPhone(string? phone)
    {
        await using var fixture = await CreateFixture();
        fixture.PatientA.Update(
            fixture.PatientA.Name,
            fixture.PatientA.Email,
            phone,
            fixture.PatientA.Address,
            fixture.PatientA.EmergencyContact,
            fixture.PatientA.Gender,
            fixture.PatientA.Notes);
        var handler = fixture.CreateWhatsAppHandler("Secretaria", Guid.Empty);

        var act = () => handler.Handle(
            new GetAppointmentWhatsAppLinkQuery(fixture.AppointmentA.Id, "UTC"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static async Task<Fixture> CreateFixture()
    {
        const string tenantId = "tenant-a";
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();
        var tenantService = new Mock<ITenantService>();
        tenantService.Setup(service => service.GetTenantId()).Returns(tenantId);
        tenantService.Setup(service => service.GetUserId()).Returns(userAId);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options, tenantService.Object);

        var patientA = new Patient(
            "Paciente A",
            "a@example.test",
            "123",
            DateTime.UtcNow.AddYears(-30),
            tenantId,
            phone: "(11) 99999-8888");
        var patientB = new Patient("Paciente B", "b@example.test", "456", DateTime.UtcNow.AddYears(-30), tenantId);
        var professionalA = new Professional("Profissional A", "111", "SP", "Clínica", tenantId, userAId);
        var professionalB = new Professional("Profissional B", "222", "SP", "Clínica", tenantId, userBId);
        var start = new DateTime(2026, 10, 15, 15, 0, 0, DateTimeKind.Utc);
        var appointmentA = new Appointment(
            start,
            start.AddMinutes(30),
            patientA.Id,
            professionalA.Id,
            AppointmentType.Consulta,
            tenantId);
        var appointmentB = new Appointment(
            start.AddHours(1),
            start.AddHours(1).AddMinutes(30),
            patientB.Id,
            professionalB.Id,
            AppointmentType.Consulta,
            tenantId);

        context.AddRange(patientA, patientB, professionalA, professionalB, appointmentA, appointmentB);
        await context.SaveChangesAsync();

        return new Fixture(
            context,
            userAId,
            professionalB,
            patientA,
            appointmentA,
            appointmentB);
    }

    private sealed record Fixture(
        ApplicationDbContext Context,
        Guid UserAId,
        Professional ProfessionalB,
        Patient PatientA,
        Appointment AppointmentA,
        Appointment AppointmentB) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Context.DisposeAsync();

        public GetAppointmentsHandler CreateHandler(string role, Guid userId)
        {
            var userService = new Mock<IUserService>();
            userService.Setup(service => service.GetUserRole()).Returns(role);
            userService.Setup(service => service.GetUserId()).Returns(userId);

            return new GetAppointmentsHandler(Context, Mock.Of<IMapper>(), userService.Object);
        }

        public GetAppointmentWhatsAppLinkHandler CreateWhatsAppHandler(string role, Guid userId)
        {
            var userService = new Mock<IUserService>();
            userService.Setup(service => service.GetUserRole()).Returns(role);
            userService.Setup(service => service.GetUserId()).Returns(userId);

            return new GetAppointmentWhatsAppLinkHandler(Context, userService.Object);
        }
    }
}
