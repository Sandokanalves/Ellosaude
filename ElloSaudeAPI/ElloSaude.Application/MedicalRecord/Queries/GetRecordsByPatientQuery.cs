using AutoMapper;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.DTOs;
using MediatR;

namespace ElloSaude.Application.MedicalRecords.Queries;

public record GetRecordsByPatientQuery(Guid PatientId) : IRequest<IEnumerable<MedicalRecordDto>>;

public class GetRecordsByPatientHandler : IRequestHandler<GetRecordsByPatientQuery, IEnumerable<MedicalRecordDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IUserService _userService;
    private readonly ITenantService _tenantService;

    public GetRecordsByPatientHandler(
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

    public async Task<IEnumerable<MedicalRecordDto>> Handle(GetRecordsByPatientQuery request, CancellationToken ct)
    {
        // Busca todos os prontuários de um paciente específico (Filtro de TenantId é automático no DBContext)
        var records = await _uow.MedicalRecords.FindAsync(
            r => r.PatientId == request.PatientId && r.DoctorId == _userService.GetUserId(),
            ct);
        var currentUser = await _uow.Users.GetByIdAsync(_userService.GetUserId(), ct);
        await _uow.AuditLogs.AddAsync(new ElloSaude.Domain.Entities.AuditLog(
            _userService.GetUserId(),
            currentUser?.Email ?? string.Empty,
            _userService.GetUserRole(),
            "Consultar prontuários do paciente",
            nameof(ElloSaude.Domain.Entities.MedicalRecord),
            request.PatientId.ToString(),
            _tenantService.GetTenantId()
        ), ct);
        await _uow.CompleteAsync(ct);
        return _mapper.Map<IEnumerable<MedicalRecordDto>>(records);
    }
}