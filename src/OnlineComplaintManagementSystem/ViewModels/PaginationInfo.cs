namespace OnlineComplaintManagementSystem.ViewModels;

public class PaginationInfo
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public Dictionary<string, string?> RouteValues { get; set; } = new();
}
