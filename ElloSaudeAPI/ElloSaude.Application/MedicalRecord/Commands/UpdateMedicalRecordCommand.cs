using ElloSaude.Application.Common.Interfaces;
using MediatR;

namespace ElloSaude.Application.MedicalRecords.Commands;

public record UpdateMedicalRecordCommand(Guid Id, string Description, string Diagnosis) : IRequest;

public class UpdateMedicalRecordHandler : IRequestHandler<UpdateMedicalRecordCommand>
{
    private readonly IUnitOfWork _uow;

    public UpdateMedicalRecordHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdateMedicalRecordCommand request, CancellationToken ct)
    {
        var record = await _uow.MedicalRecords.GetByIdAsync(request.Id, ct);
        if (record == null) throw new Exception("Prontuário não encontrado.");

        record.Update(request.Description, request.Diagnosis);
        _uow.MedicalRecords.Update(record);
        await _uow.CompleteAsync(ct);
    }
}