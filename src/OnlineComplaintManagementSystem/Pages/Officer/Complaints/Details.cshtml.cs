using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;
using OnlineComplaintManagementSystem.Services;
using OnlineComplaintManagementSystem.ViewModels;

namespace OnlineComplaintManagementSystem.Pages.Officer.Complaints;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IComplaintService _complaintService;
    private readonly IFileUploadService _fileUploadService;

    public DetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IComplaintService complaintService, IFileUploadService fileUploadService)
    {
        _context = context;
        _userManager = userManager;
        _complaintService = complaintService;
        _fileUploadService = fileUploadService;
    }

    public ComplaintDetailsViewModel Vm { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var complaint = await LoadAssignedComplaintAsync(id);
        if (complaint is null) return NotFound();

        Vm = BuildViewModel(complaint);
        return Page();
    }

    private static readonly HashSet<ComplaintStatus> OfficerAllowedStatuses = new() { ComplaintStatus.UnderReview, ComplaintStatus.Investigating };

    public async Task<IActionResult> OnPostChangeStatusAsync(int id, ComplaintStatus status, string? notes)
    {
        if (await LoadAssignedComplaintAsync(id) is null) return NotFound();
        if (!OfficerAllowedStatuses.Contains(status)) return Forbid();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Forbid();

        await _complaintService.ChangeStatusAsync(id, status, user.Id, user.FullName, notes);
        TempData["SuccessMessage"] = "Complaint status updated.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostEscalateAsync(int id, string reason)
    {
        if (await LoadAssignedComplaintAsync(id) is null) return NotFound();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Forbid();

        await _complaintService.EscalateAsync(id, user.Id, user.FullName, reason);
        TempData["SuccessMessage"] = "Complaint has been escalated.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostResolveAsync(int id, string resolutionSummary)
    {
        if (await LoadAssignedComplaintAsync(id) is null) return NotFound();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Forbid();

        await _complaintService.ResolveAsync(id, user.Id, user.FullName, resolutionSummary);
        TempData["SuccessMessage"] = "Complaint marked as resolved.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReplyAsync(int id, string message, bool isInformationRequest, IFormFile? uploadFile)
    {
        if (await LoadAssignedComplaintAsync(id) is null) return NotFound();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Forbid();

        var files = uploadFile is not null ? new List<IFormFile> { uploadFile } : new List<IFormFile>();
        await _complaintService.AddResponseAsync(id, user.Id, user.FullName, ResponseAuthorType.ComplaintOfficer, message, isInformationRequest, files);

        TempData["SuccessMessage"] = "Message sent.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUploadAttachmentAsync(int id, IFormFile uploadFile)
    {
        if (await LoadAssignedComplaintAsync(id) is null) return NotFound();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Forbid();

        var result = await _fileUploadService.SaveComplaintAttachmentAsync(uploadFile, id);
        if (result.Success)
        {
            _context.ComplaintAttachments.Add(new ComplaintAttachment
            {
                ComplaintId = id,
                FileName = result.OriginalFileName!,
                StoredPath = result.StoredPath!,
                ContentType = result.ContentType!,
                FileSizeBytes = result.FileSizeBytes,
                UploadedById = user.Id,
                UploadedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Document uploaded successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }

        return RedirectToPage(new { id });
    }

    private async Task<Complaint?> LoadAssignedComplaintAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        return await _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.Department)
            .Include(c => c.Complainant)
            .Include(c => c.Attachments)
            .Include(c => c.Assignments).ThenInclude(a => a.Officer)
            .Include(c => c.StatusHistory).ThenInclude(h => h.ChangedBy)
            .Include(c => c.Responses).ThenInclude(r => r.Author)
            .Include(c => c.Responses).ThenInclude(r => r.Attachments)
            .Include(c => c.Feedback)
            .Where(c => c.Id == id && c.Assignments.Any(a => a.OfficerId == userId && a.UnassignedDate == null))
            .FirstOrDefaultAsync();
    }

    private static ComplaintDetailsViewModel BuildViewModel(Complaint complaint) => new()
    {
        Complaint = complaint,
        CanChangeStatus = true,
        CanEscalate = true,
        CanResolve = true,
        CanReply = true,
        CanRequestInfo = true,
        CanUploadAttachment = true,
        ShowContactInfo = true
    };
}
