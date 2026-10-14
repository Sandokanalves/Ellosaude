using System.Globalization;
using ElloSaude.Application.Common.Exceptions;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.DTOs;
using ElloSaude.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Appointments.Queries;

public sealed record GetAppointmentWhatsAppLinkQuery(
    Guid AppointmentId,
    string TimeZoneId) : IRequest<WhatsAppConfirmationLinkDto>;

public sealed class GetAppointmentWhatsAppLinkValidator
    : AbstractValidator<GetAppointmentWhatsAppLinkQuery>
{
    public GetAppointmentWhatsAppLinkValidator()
    {
        RuleFor(query => query.AppointmentId).NotEmpty();
        RuleFor(query => query.TimeZoneId).NotEmpty().MaximumLength(100);
    }
}

public sealed class GetAppointmentWhatsAppLinkHandler
    : IRequestHandler<GetAppointmentWhatsAppLinkQuery, WhatsAppConfirmationLinkDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserService _userService;

    public GetAppointmentWhatsAppLinkHandler(
        IApplicationDbContext context,
        IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task<WhatsAppConfirmationLinkDto> Handle(
        GetAppointmentWhatsAppLinkQuery request,
        CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(item => item.Id == request.AppointmentId, cancellationToken);
        if (appointment is null)
            throw new KeyNotFoundException("Agendamento não encontrado.");

        if (_userService.GetUserRole() == "Profissional")
        {
            var userId = _userService.GetUserId();
            var ownsAppointment = userId != Guid.Empty
                && await _context.Professionals.AnyAsync(
                    professional => professional.Id == appointment.ProfessionalId
                        && professional.UserId == userId
                        && professional.IsActive,
                    cancellationToken);
            if (!ownsAppointment)
                throw new KeyNotFoundException("Agendamento não encontrado.");
        }

        if (appointment.Status is not (AppointmentStatus.Pendente or AppointmentStatus.Confirmado))
            throw new InvalidOperationException(
                "O link de confirmação só pode ser gerado para agendamentos pendentes ou confirmados.");

        var patient = await _context.Patients
            .FirstOrDefaultAsync(item => item.Id == appointment.PatientId, cancellationToken);
        if (patient is null)
            throw new KeyNotFoundException("Paciente do agendamento não encontrado.");
        if (string.IsNullOrWhiteSpace(patient.Phone))
            throw new InvalidOperationException("Cadastre um telefone do paciente antes de gerar o link do WhatsApp.");

        var professional = await _context.Professionals
            .FirstOrDefaultAsync(item => item.Id == appointment.ProfessionalId, cancellationToken);
        if (professional is null)
            throw new KeyNotFoundException("Profissional do agendamento não encontrado.");

        var timeZone = FindTimeZone(request.TimeZoneId);
        var appointmentStartUtc = new DateTimeOffset(
            DateTime.SpecifyKind(appointment.StartTime, DateTimeKind.Utc));
        var localStart = TimeZoneInfo.ConvertTime(appointmentStartUtc, timeZone);
        var formattedStart = localStart.ToString(
            "dd/MM/yyyy 'às' HH:mm",
            CultureInfo.GetCultureInfo("pt-BR"));
        var message =
            $"Olá, {patient.Name}! Lembramos da sua consulta com {professional.Name} em {formattedStart}. " +
            "Para confirmar, responda 1. Para solicitar cancelamento ou reagendamento, responda 2.";
        var phoneNumber = NormalizeBrazilianPhone(patient.Phone);

        return new WhatsAppConfirmationLinkDto(
            $"https://wa.me/{phoneNumber}?text={Uri.EscapeDataString(message)}");
    }

    private static TimeZoneInfo FindTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw InvalidTimeZone(timeZoneId);
        }
        catch (InvalidTimeZoneException)
        {
            throw InvalidTimeZone(timeZoneId);
        }
    }

    private static ElloSaude.Application.Common.Exceptions.ValidationException InvalidTimeZone(
        string timeZoneId) =>
        new(
        [
            new ValidationFailure(
                nameof(GetAppointmentWhatsAppLinkQuery.TimeZoneId),
                $"O fuso horário '{timeZoneId}' não é válido.")
        ]);

    private static string NormalizeBrazilianPhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length is 10 or 11)
            return $"55{digits}";
        if (digits.Length is 12 or 13 && digits.StartsWith("55", StringComparison.Ordinal))
            return digits;

        throw new InvalidOperationException(
            "O telefone do paciente deve conter DDD e um número brasileiro válido.");
    }
}
