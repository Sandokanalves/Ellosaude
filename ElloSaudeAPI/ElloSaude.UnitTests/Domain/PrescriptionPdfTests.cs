using ElloSaude.Application.Prescriptions.Queries;
using ElloSaude.Infrastructure.Documents;
using FluentAssertions;
using QuestPDF.Infrastructure;
using Xunit;

namespace ElloSaude.UnitTests.Domain;

public class PrescriptionPdfTests
{
    [Fact]
    public void Generate_ShouldReturnPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var service = new QuestPdfPrescriptionPdfService();
        var prescription = new PrescriptionDetailsDto(
            Guid.NewGuid(),
            "Paciente Teste",
            new DateTime(1990, 1, 1),
            "Dra. Teste",
            "123456",
            "SP",
            "Clínica",
            new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc),
            "Tomar com água",
            new[]
            {
                new PrescriptionItemDetailsDto(
                    "Medicamento",
                    "1 comprimido",
                    "a cada 8 horas",
                    "5 dias",
                    "oral",
                    null)
            });

        var pdf = service.Generate(prescription);

        System.Text.Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
    }
}
