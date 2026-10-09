using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Professionals.Commands;

public record CreateProfessionalCommand(
    string Name,
    string Crm,
    string CrmState,
    string Specialty,
    string? Phone,
    string? Email,
    decimal ConsultationPrice,
    Guid? UserId
) : IRequest<Guid>;

public class CreateProfessionalCommandValidator : AbstractValidator<CreateProfessionalCommand>
{
    public CreateProfessionalCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage("Nome é obrigatório e deve ter no máximo 200 caracteres.");
        RuleFor(x => x.Crm).NotEmpty().MaximumLength(20).WithMessage("CRM é obrigatório.");
        RuleFor(x => x.CrmState).NotEmpty().Length(2).WithMessage("Estado do CRM deve ter 2 letras.");
        RuleFor(x => x.Specialty).NotEmpty().MaximumLength(100).WithMessage("Especialidade é obrigatória.");
        RuleFor(x => x.ConsultationPrice).GreaterThanOrEqualTo(0).WithMessage("Preço da consulta não pode ser negativo.");
        RuleFor(x => x.Phone).MaximumLength(20).When(x => x.Phone != null);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => x.Email != null);
    }
}

public class CreateProfessionalCommandHandler : IRequestHandler<CreateProfessionalCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateProfessionalCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateProfessionalCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetTenantId();

        // Verifica unicidade do CRM dentro do tenant
        var crmExists = await _context.Professionals
            .AnyAsync(p => p.Crm == request.Crm && p.CrmState == request.CrmState, cancellationToken);

        if (crmExists)
            throw new InvalidOperationException($"Já existe um profissional com o CRM {request.Crm}/{request.CrmState} nesta clínica.");

        var professional = new Professional(
            name: request.Name,
            crm: request.Crm,
            crmState: request.CrmState,
            specialty: request.Specialty,
            tenantId: tenantId,
            userId: request.UserId,
            phone: request.Phone,
            email: request.Email,
            consultationPrice: request.ConsultationPrice
        );

        _context.Professionals.Add(professional);
        await _context.SaveChangesAsync(cancellationToken);

        return professional.Id;
    }
}
