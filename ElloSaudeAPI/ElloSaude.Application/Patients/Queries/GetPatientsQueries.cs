using MediatR;
using ElloSaude.Application.DTOs;
using ElloSaude.Application.Common.Interfaces;
using AutoMapper;

namespace ElloSaude.Application.Patients.Queries;

public record GetAllPatientsQuery() : IRequest<IEnumerable<PatientDto>>;
public record GetPatientByIdQuery(Guid Id) : IRequest<PatientDto>;

public class GetPatientsHandler :
    IRequestHandler<GetAllPatientsQuery, IEnumerable<PatientDto>>,
    IRequestHandler<GetPatientByIdQuery, PatientDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public GetPatientsHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<IEnumerable<PatientDto>> Handle(GetAllPatientsQuery request, CancellationToken ct)
    {
        var patients = await _uow.Patients.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<PatientDto>>(patients);
    }

    public async Task<PatientDto> Handle(GetPatientByIdQuery request, CancellationToken ct)
    {
        var patient = await _uow.Patients.GetByIdAsync(request.Id, ct);
        if (patient == null)
            throw new KeyNotFoundException("Paciente não encontrado.");

        return _mapper.Map<PatientDto>(patient);
    }
}
