using ElloSaude.Application.Common.Interfaces;
using MediatR;

namespace ElloSaude.Application.Patients.Commands;

public record DeletePatientCommand(Guid Id) : IRequest;

public class DeletePatientHandler : IRequestHandler<DeletePatientCommand>
{
    private readonly IUnitOfWork _uow;

    public DeletePatientHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(DeletePatientCommand request, CancellationToken ct)
    {
        var patient = await _uow.Patients.GetByIdAsync(request.Id, ct);
        if (patient == null)
            throw new KeyNotFoundException("Paciente não encontrado.");

        _uow.Patients.Delete(patient);
        await _uow.CompleteAsync(ct);
    }
}
