using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.Prescriptions.Queries;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ElloSaude.Infrastructure.Documents;

public sealed class QuestPdfPrescriptionPdfService : IPrescriptionPdfService
{
    public byte[] Generate(PrescriptionDetailsDto prescription)
    {
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(style => style.FontSize(10));

                page.Header().Column(header =>
                {
                    header.Item().Text("ElloSaúde").FontSize(18).Bold();
                    header.Item().Text("Receita médica").FontSize(14).Bold();
                });

                page.Content().PaddingVertical(20).Column(content =>
                {
                    content.Spacing(8);
                    content.Item().Text($"Paciente: {prescription.PatientName}");
                    content.Item().Text($"Nascimento: {prescription.PatientBirthDate:dd/MM/yyyy}");
                    content.Item().Text($"Profissional: {prescription.ProfessionalName}");
                    content.Item().Text($"CRM: {prescription.Crm}/{prescription.CrmState}  |  Especialidade: {prescription.Specialty}");
                    content.Item().Text($"Emitida em: {prescription.IssueDate:dd/MM/yyyy HH:mm} UTC");
                    content.Item().PaddingTop(8).Text("Medicamentos").Bold();

                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(StyleCell).Text("Medicamento");
                            header.Cell().Element(StyleCell).Text("Posologia");
                            header.Cell().Element(StyleCell).Text("Duração / via");
                        });

                        foreach (var item in prescription.Items)
                        {
                            table.Cell().Element(StyleCell).Text(item.MedicationName);
                            table.Cell().Element(StyleCell).Text($"{item.Dosage}; {item.Frequency}");
                            table.Cell().Element(StyleCell).Text($"{item.Duration}; {item.Route}");

                            if (!string.IsNullOrWhiteSpace(item.Instructions))
                            {
                                table.Cell().ColumnSpan(3).Element(StyleCell)
                                    .Text($"Instruções: {item.Instructions}");
                            }
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(prescription.Notes))
                        content.Item().PaddingTop(12).Text($"Orientações: {prescription.Notes}");

                    content.Item().PaddingTop(24)
                        .Text($"Documento {prescription.Id:N}")
                        .FontColor(Colors.Grey.Darken1);
                });

                page.Footer().AlignCenter()
                    .Text("Documento clínico confidencial.")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();
    }

    private static IContainer StyleCell(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
}
