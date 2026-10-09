using ElloSaude.Application.DTOs;
using ElloSaude.Application.Common.Interfaces;
using AutoMapper;
using MediatR;

namespace ElloSaude.Application.MedicalRecords.Queries;

public record GetMedicalRecordByIdQuery(Guid Id) : IRequest<MedicalRecordDto>;

public class GetMedicalRecordByIdHandler : IRequestHandler<GetMedicalRecordByIdQuery, MedicalRecordDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IUserService _userService;
    private readonly ITenantService _tenantService;

    public GetMedicalRecordByIdHandler(
        IUnitOfWork uow,
        IMapper mapper,
        IUserService userService,
        ITenantService tenantService)
    {
        _uow = uow;
        _mapper = mapper;
        _userService = userService;
        _tenantService = tenantService;
    }

    public async Task<MedicalRecordDto> Handle(GetMedicalRecordByIdQuery request, CancellationToken ct)
    {
        var record = await _uow.MedicalRecords.GetByIdAsync(request.Id, ct);
        if (record == null || record.DoctorId != _userService.GetUserId())
            throw new KeyNotFoundException("Prontuário não encontrado.");

        var currentUser = await _uow.Users.GetByIdAsync(_userService.GetUserId(), ct);
        await _uow.AuditLogs.AddAsync(new ElloSaude.Domain.Entities.AuditLog(
            _userService.GetUserId(),
            currentUser?.Email ?? string.Empty,
            _userService.GetUserRole(),
            "Acessar prontuário",
            nameof(ElloSaude.Domain.Entities.MedicalRecord),
            record.Id.ToString(),
            _tenantService.GetTenantId()
        ), ct);
        await _uow.CompleteAsync(ct);
        return _mapper.Map<MedicalRecordDto>(record);
    }
}