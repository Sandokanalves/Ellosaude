namespace ElloSaude.Application.DTOs;

public record FinancialSummaryDto(
    decimal TotalReceived,
    decimal TotalPending,
    decimal TotalRevenue,
    int CompletedAppointments,
    int PendingAppointments
);
