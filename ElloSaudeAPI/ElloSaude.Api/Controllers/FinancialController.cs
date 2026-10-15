using ElloSaude.Application.Financial.Commands;
using ElloSaude.Application.Financial.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElloSaude.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FinancialController : ControllerBase
{
    private readonly IMediator _mediator;

    public FinancialController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Resumo financeiro simplificado (totais por período).
    /// </summary>
    [HttpGet("summary")]
    [Authorize(Roles = "Admin,Profissional,Secretaria")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var result = await _mediator.Send(new GetFinancialSummaryQuery(startDate, endDate));
        return Ok(result);
    }

    /// <summary>
    /// Relatório financeiro detalhado: por forma de pagamento, por profissional e lista de transações.
    /// </summary>
    [HttpGet("report")]
    [Authorize(Roles = "Admin,Profissional")]
    public async Task<IActionResult> GetReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? professionalId)
    {
        var result = await _mediator.Send(new GetFinancialReportQuery(startDate, endDate, professionalId));
        return Ok(result);
    }

    /// <summary>
    /// Retorna somente lançamentos pendentes para operação de recebimento pela equipe da clínica.
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Admin,Secretaria")]
    public async Task<IActionResult> GetPendingPayments(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetPendingPaymentsQuery(startDate, endDate),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Registra o recebimento de uma consulta.
    /// Requer perfil Admin ou Secretaria.
    /// </summary>
    [HttpPost("{paymentRecordId:guid}/pay")]
    [Authorize(Roles = "Admin,Secretaria")]
    public async Task<IActionResult> RegisterPayment(
        Guid paymentRecordId,
        [FromBody] RegisterPaymentRequest request)
    {
        await _mediator.Send(new RegisterPaymentCommand(
            PaymentRecordId: paymentRecordId,
            AmountPaid: request.AmountPaid,
            PaymentMethodId: request.PaymentMethodId,
            Notes: request.Notes
        ));
        return Ok(new { message = "Pagamento registrado com sucesso." });
    }

    /// <summary>
    /// Marca uma consulta como retorno gratuito, zerando o valor a pagar.
    /// </summary>
    [HttpPost("{paymentRecordId:guid}/free-return")]
    [Authorize(Roles = "Admin,Profissional")]
    public async Task<IActionResult> MarkAsFreeReturn(
        Guid paymentRecordId,
        [FromBody] FreeReturnRequest request)
    {
        await _mediator.Send(new MarkAsFreeReturnCommand(
            PaymentRecordId: paymentRecordId,
            Reason: request.Reason
        ));
        return Ok(new { message = "Consulta marcada como retorno gratuito." });
    }

    [HttpPost("{paymentRecordId:guid}/cancel")]
    [Authorize(Roles = "Admin,Secretaria")]
    public async Task<IActionResult> CancelPayment(
        Guid paymentRecordId,
        [FromBody] CancelPaymentRequest request)
    {
        await _mediator.Send(new CancelPaymentCommand(paymentRecordId, request.Reason));
        return Ok(new { message = "Lançamento financeiro cancelado." });
    }

    // --- Controle de Despesas da Clínica ---

    [HttpGet("expenses")]
    [Authorize(Roles = "Admin,Secretaria")]
    public async Task<IActionResult> GetExpenses([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? category)
    {
        var result = await _mediator.Send(new GetExpensesQuery(startDate, endDate, category));
        return Ok(result);
    }

    [HttpPost("expenses")]
    [Authorize(Roles = "Admin,Secretaria")]
    public async Task<IActionResult> CreateExpense([FromBody] CreateClinicExpenseCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { Id = id, message = "Despesa lançada com sucesso." });
    }

    [HttpPatch("expenses/{id:guid}/pay")]
    [Authorize(Roles = "Admin,Secretaria")]
    public async Task<IActionResult> PayExpense(Guid id, [FromBody] PayExpenseRequest? request)
    {
        await _mediator.Send(new PayClinicExpenseCommand(id, request?.PaidDate, request?.PaymentMethod));
        return NoContent();
    }

    [HttpDelete("expenses/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteExpense(Guid id)
    {
        await _mediator.Send(new DeleteClinicExpenseCommand(id));
        return NoContent();
    }

    // --- Caixa Diário (Abertura e Fechamento) ---

    [HttpGet("cash-register/current")]
    [Authorize(Roles = "Admin,Secretaria")]
    public async Task<IActionResult> GetCurrentCashRegister()
    {
        var result = await _mediator.Send(new GetCurrentCashRegisterQuery());
        return Ok(result);
    }

    [HttpPost("cash-register/open")]
    [Authorize(Roles = "Admin,Secretaria")]
    public async Task<IActionResult> OpenCashRegister([FromBody] OpenCashRegisterCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { Id = id, message = "Caixa diário aberto com sucesso." });
    }

    [HttpPost("cash-register/close")]
    [Authorize(Roles = "Admin,Secretaria")]
    public async Task<IActionResult> CloseCashRegister([FromBody] CloseCashRegisterCommand command)
    {
        await _mediator.Send(command);
        return Ok(new { message = "Caixa diário fechado com sucesso." });
    }

    // --- DRE Simplificado / Relatório Gerencial ---

    [HttpGet("dre")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDreReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var result = await _mediator.Send(new GetDreReportQuery(startDate, endDate));
        return Ok(result);
    }
}

public record PayExpenseRequest(DateTime? PaidDate, string? PaymentMethod);

public record RegisterPaymentRequest(
    decimal AmountPaid,
    int PaymentMethodId,
    string? Notes
);

public record FreeReturnRequest(
    string? Reason
);

public record CancelPaymentRequest(string Reason);
