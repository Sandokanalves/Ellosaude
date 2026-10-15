namespace ElloSaude.Application.DTOs;

public record DreCategoryAmountDto(string Category, decimal Amount);

public record DreReportDto(
    DateTime StartDate,
    DateTime EndDate,
    decimal GrossRevenue,
    decimal TotalCommissions,
    decimal TotalExpenses,
    decimal NetProfit,
    decimal ProfitMarginPercentage,
    IReadOnlyList<DreCategoryAmountDto> RevenueByMethod,
    IReadOnlyList<DreCategoryAmountDto> ExpensesByCategory
);
