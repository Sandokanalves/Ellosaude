namespace ElloSaude.Application.DTOs;

public record ClinicExpenseDto(
    Guid Id,
    string Description,
    string Category,
    decimal Amount,
    DateTime DueDate,
    DateTime? PaidDate,
    bool IsPaid,
    string? PaymentMethod,
    string? Notes,
    DateTime CreatedAt
);
