using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OnlineComplaintManagementSystem.Services;

public class ReportExportService : IReportExportService
{
    public byte[] ExportToExcel(string reportTitle, List<ComplaintReportRow> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Report");

        sheet.Cell(1, 1).Value = reportTitle;
        sheet.Range(1, 1, 1, 9).Merge();
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;

        sheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";
        sheet.Range(2, 1, 2, 9).Merge();

        string[] headers = { "Reference No.", "Title", "Category", "Department", "Priority", "Status", "Assigned Officer", "Submitted Date", "Resolved Date" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(4, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F3864");
            cell.Style.Font.FontColor = XLColor.White;
        }

        var row = 5;
        foreach (var r in rows)
        {
            sheet.Cell(row, 1).Value = r.ReferenceNumber;
            sheet.Cell(row, 2).Value = r.Title;
            sheet.Cell(row, 3).Value = r.Category;
            sheet.Cell(row, 4).Value = r.Department;
            sheet.Cell(row, 5).Value = r.Priority;
            sheet.Cell(row, 6).Value = r.Status;
            sheet.Cell(row, 7).Value = r.AssignedOfficer;
            sheet.Cell(row, 8).Value = r.SubmittedDate.ToLocalTime().ToString("yyyy-MM-dd");
            sheet.Cell(row, 9).Value = r.ResolvedDate.HasValue ? r.ResolvedDate.Value.ToLocalTime().ToString("yyyy-MM-dd") : "-";
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportToPdf(string reportTitle, List<ComplaintReportRow> rows)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text(reportTitle).FontSize(16).Bold();
                    col.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.3f);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.6f);
                        columns.RelativeColumn(0.9f);
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1f);
                    });

                    table.Header(header =>
                    {
                        foreach (var text in new[] { "Reference No.", "Title", "Category", "Department", "Priority", "Status", "Officer", "Submitted", "Resolved" })
                        {
                            header.Cell().Background(Colors.Blue.Darken3).Padding(4)
                                .Text(text).FontColor(Colors.White).Bold();
                        }
                        header.Cell().ColumnSpan(9).PaddingTop(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    foreach (var r in rows)
                    {
                        table.Cell().Padding(4).Text(r.ReferenceNumber);
                        table.Cell().Padding(4).Text(r.Title);
                        table.Cell().Padding(4).Text(r.Category);
                        table.Cell().Padding(4).Text(r.Department);
                        table.Cell().Padding(4).Text(r.Priority);
                        table.Cell().Padding(4).Text(r.Status);
                        table.Cell().Padding(4).Text(r.AssignedOfficer);
                        table.Cell().Padding(4).Text(r.SubmittedDate.ToLocalTime().ToString("yyyy-MM-dd"));
                        table.Cell().Padding(4).Text(r.ResolvedDate.HasValue ? r.ResolvedDate.Value.ToLocalTime().ToString("yyyy-MM-dd") : "-");
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
