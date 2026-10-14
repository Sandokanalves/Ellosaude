using AutoMapper;
using ElloSaude.Application.Appointments.Queries;
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

        var patientA = new Patient("Paciente A", "a@example.test", "123", DateTime.UtcNow.AddYears(-30), tenantId);
        var patientB = new Patient("Paciente B", "b@example.test", "456", DateTime.UtcNow.AddYears(-30), tenantId);
        var professionalA = new Professional("Profissional A", "111", "SP", "Clínica", tenantId, userAId);
        var professionalB = new Professional("Profissional B", "222", "SP", "Clínica", tenantId, userBId);
        var start = DateTime.UtcNow.AddDays(1);
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
            appointmentA,
            appointmentB);
    }

    private sealed record Fixture(
        ApplicationDbContext Context,
        Guid UserAId,
        Professional ProfessionalB,
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
    }
}
