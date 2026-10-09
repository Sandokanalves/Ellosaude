using ElloSaude.Application.Prescriptions.Queries;

namespace ElloSaude.Application.Common.Interfaces;

public interface IPrescriptionPdfService
{
    byte[] Generate(PrescriptionDetailsDto prescription);
}
