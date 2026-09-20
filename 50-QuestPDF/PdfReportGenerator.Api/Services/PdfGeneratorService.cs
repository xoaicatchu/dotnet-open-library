using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using PdfReportGenerator.Api.Documents;
using PdfReportGenerator.Api.Models;

namespace PdfReportGenerator.Api.Services;

public class PdfGeneratorService
{
    static PdfGeneratorService()
    {
        // QuestPDF requires Community license registration for non-commercial or small business use
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = false;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
    }

    public byte[] GenerateInvoicePdf(InvoiceDto invoice)
    {
        var document = new InvoiceDocument(invoice);
        return document.GeneratePdf();
    }

    public PdfMetadataDto GetInvoiceMetadata(InvoiceDto invoice)
    {
        var document = new InvoiceDocument(invoice);
        var bytes = document.GeneratePdf();

        return new PdfMetadataDto(
            invoice.InvoiceNumber,
            1, // Standard single-page invoice for typical line item counts
            bytes.Length,
            "application/pdf");
    }
}
