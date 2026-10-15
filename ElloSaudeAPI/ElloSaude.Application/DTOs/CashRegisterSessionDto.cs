namespace ElloSaude.Application.DTOs;

public record CashRegisterSessionDto(
    Guid Id,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    decimal OpeningBalance,
    decimal? ClosingBalance,
    string Status,
    Guid OpenedByUserId,
    Guid? ClosedByUserId,
    string? Notes,
    decimal TotalReceivedToday = 0,
    decimal CurrentBalance = 0
);
