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

    public GetRecordsByPatientHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<IEnumerable<MedicalRecordDto>> Handle(GetRecordsByPatientQuery request, CancellationToken ct)
    {
        // Busca todos os prontuários de um paciente específico (Filtro de TenantId é automático no DBContext)
        var records = await _uow.MedicalRecords.FindAsync(r => r.PatientId == request.PatientId, ct);
        return _mapper.Map<IEnumerable<MedicalRecordDto>>(records);
    }
}