namespace OnlineComplaintManagementSystem.Services;

public interface IReportExportService
{
    byte[] ExportToExcel(string reportTitle, List<ComplaintReportRow> rows);
    byte[] ExportToPdf(string reportTitle, List<ComplaintReportRow> rows);
}
