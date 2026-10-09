using ElloSaude.Application.Common.Interfaces;
using MediatR;

namespace ElloSaude.Application.MedicalRecords.Commands;

public record DeleteMedicalRecordCommand(Guid Id) : IRequest;

public class DeleteMedicalRecordHandler : IRequestHandler<DeleteMedicalRecordCommand>
{
    private readonly IUnitOfWork _uow;
    public DeleteMedicalRecordHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(DeleteMedicalRecordCommand request, CancellationToken ct)
    {
        var record = await _uow.MedicalRecords.GetByIdAsync(request.Id, ct);
        if (record != null)
        {
            _uow.MedicalRecords.Delete(record);
            await _uow.CompleteAsync(ct);
        }
    }
}