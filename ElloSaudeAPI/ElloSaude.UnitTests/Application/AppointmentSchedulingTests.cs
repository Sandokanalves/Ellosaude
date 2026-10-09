using ElloSaude.Application.Appointments.Commands;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.Professionals.Queries;
using ElloSaude.Domain.Entities;
using ElloSaude.Domain.Enums;
using ElloSaude.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ElloSaude.UnitTests.Application;

public class AppointmentSchedulingTests
{
    [Fact]
    public async Task CreateAppointment_ShouldRejectOverlappingProfessionalSchedule()
    {
        var tenantId = "tenant-a";
        var userId = Guid.NewGuid();
        await using var context = CreateContext(tenantId, Guid.NewGuid().ToString(), userId);
        var (patient, professional, start) = await SeedAvailability(context, tenantId);
        context.Appointments.Add(new Appointment(
            start,
            start.AddHours(1),
            patient.Id,
            professional.Id,
            AppointmentType.Consulta,
            tenantId));
        await context.SaveChangesAsync();

        var handler = new CreateAppointmentHandler(
            context,
            CreateTenantService(tenantId, userId).Object);
        var command = new CreateAppointmentCommand(
            patient.Id,
            professional.Id,
            start.AddMinutes(30),
            start.AddHours(1).AddMinutes(30),
            (int)AppointmentType.Consulta);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Conflito de horário*");
    }

    [Fact]
    public async Task CreateAppointment_ShouldRejectWhenAvailabilityIsNotConfigured()
    {
        var tenantId = "tenant-a";
        var userId = Guid.NewGuid();
        await using var context = CreateContext(tenantId, Guid.NewGuid().ToString(), userId);
        var start = NextWeekdayAt(DayOfWeek.Monday, 10);
        var patient = new Patient("Paciente", "patient@example.test", "123", start.AddYears(-30), tenantId);
        var professional = new Professional("Dra. Teste", "123", "SP", "Clínica", tenantId);
        context.AddRange(patient, professional);
        await context.SaveChangesAsync();

        var handler = new CreateAppointmentHandler(
            context,
            CreateTenantService(tenantId, userId).Object);
        var command = new CreateAppointmentCommand(
            patient.Id,
            professional.Id,
            start,
            start.AddMinutes(30),
            (int)AppointmentType.Consulta);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*não possui disponibilidade configurada*");
    }

    [Fact]
    public async Task GetAvailableSlots_ShouldMarkOnlySlotsOverlappingPartialBlockUnavailable()
    {
        var tenantId = "tenant-a";
        var userId = Guid.NewGuid();
        await using var context = CreateContext(tenantId, Guid.NewGuid().ToString(), userId);
        var (patient, professional, start) = await SeedAvailability(
            context,
            tenantId,
            DayOfWeek.Monday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(10),
            slotDurationMinutes: 30);
        context.ScheduleBlocks.Add(new ScheduleBlock(
            professional.Id,
            start.AddMinutes(5),
            start.AddMinutes(15),
            "Bloqueio parcial",
            tenantId));
        await context.SaveChangesAsync();

        var handler = new GetAvailableSlotsQueryHandler(context);
        var result = (await handler.Handle(
            new GetAvailableSlotsQuery(professional.Id, DateOnly.FromDateTime(start)),
            CancellationToken.None)).ToArray();

        result.Should().HaveCount(2);
        result[0].IsAvailable.Should().BeFalse();
        result[1].IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task TenantQueryFilter_ShouldUseTheCurrentContextTenant()
    {
        var databaseName = Guid.NewGuid().ToString();
        var tenantAContext = CreateContext("tenant-a", databaseName, Guid.NewGuid());
        var patientA = new Patient("Paciente A", "a@example.test", "123", DateTime.UtcNow.AddYears(-30), "tenant-a");
        tenantAContext.Patients.Add(patientA);
        await tenantAContext.SaveChangesAsync();
        await tenantAContext.DisposeAsync();

        var tenantBContext = CreateContext("tenant-b", databaseName, Guid.NewGuid());
        var patientB = new Patient("Paciente B", "b@example.test", "456", DateTime.UtcNow.AddYears(-30), "tenant-b");
        tenantBContext.Patients.Add(patientB);
        await tenantBContext.SaveChangesAsync();
        await tenantBContext.DisposeAsync();

        await using var currentTenantAContext = CreateContext("tenant-a", databaseName, Guid.NewGuid());
        await using var currentTenantBContext = CreateContext("tenant-b", databaseName, Guid.NewGuid());

        (await currentTenantAContext.Patients.Select(patient => patient.Name).ToListAsync())
            .Should().Equal("Paciente A");
        (await currentTenantBContext.Patients.Select(patient => patient.Name).ToListAsync())
            .Should().Equal("Paciente B");
    }

    private static async Task<(Patient Patient, Professional Professional, DateTime Start)> SeedAvailability(
        ApplicationDbContext context,
        string tenantId,
        DayOfWeek dayOfWeek = DayOfWeek.Monday,
        TimeSpan? startTime = null,
        TimeSpan? endTime = null,
        int slotDurationMinutes = 30)
    {
        var start = NextWeekdayAt(dayOfWeek, (startTime ?? TimeSpan.FromHours(9)).Hours);
        var patient = new Patient("Paciente", "patient@example.test", "123", start.AddYears(-30), tenantId);
        var professional = new Professional("Dra. Teste", "123", "SP", "Clínica", tenantId);
        context.AddRange(patient, professional);
        context.DoctorAvailabilities.Add(new DoctorAvailability(
            professional.Id,
            dayOfWeek,
            startTime ?? TimeSpan.FromHours(9),
            endTime ?? TimeSpan.FromHours(17),
            tenantId,
            slotDurationMinutes: slotDurationMinutes));
        await context.SaveChangesAsync();
        return (patient, professional, start);
    }

    private static DateTime NextWeekdayAt(DayOfWeek dayOfWeek, int hour)
    {
        var daysAhead = ((int)dayOfWeek - (int)DateTime.UtcNow.DayOfWeek + 7) % 7;
        if (daysAhead == 0)
            daysAhead = 7;
        return DateTime.UtcNow.Date.AddDays(daysAhead).AddHours(hour);
    }

    private static ApplicationDbContext CreateContext(string tenantId, string databaseName, Guid userId)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new ApplicationDbContext(options, CreateTenantService(tenantId, userId).Object);
    }

    private static Mock<ITenantService> CreateTenantService(string tenantId, Guid userId)
    {
        var tenantService = new Mock<ITenantService>();
        tenantService.Setup(service => service.GetTenantId()).Returns(tenantId);
        tenantService.Setup(service => service.GetUserId()).Returns(userId);
        return tenantService;
    }
}
