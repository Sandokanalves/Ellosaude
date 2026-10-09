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

    public GetMedicalRecordByIdHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<MedicalRecordDto> Handle(GetMedicalRecordByIdQuery request, CancellationToken ct)
    {
        var record = await _uow.MedicalRecords.GetByIdAsync(request.Id, ct);
        return _mapper.Map<MedicalRecordDto>(record);
    }
}