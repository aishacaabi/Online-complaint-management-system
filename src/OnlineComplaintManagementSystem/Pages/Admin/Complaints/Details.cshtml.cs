using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;
using OnlineComplaintManagementSystem.Services;
using OnlineComplaintManagementSystem.ViewModels;

namespace OnlineComplaintManagementSystem.Pages.Admin.Complaints;

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
        var complaint = await LoadComplaintAsync(id);
        if (complaint is null) return NotFound();

        Vm = await BuildViewModelAsync(complaint);
        return Page();
    }

    public async Task<IActionResult> OnPostAssignAsync(int id, string officerId, string? notes)
    {
        if (await LoadComplaintAsync(id) is null) return NotFound();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Forbid();

        if (string.IsNullOrEmpty(officerId))
        {
            TempData["ErrorMessage"] = "Please select an officer to assign.";
            return RedirectToPage(new { id });
        }

        var officer = await _userManager.FindByIdAsync(officerId);
        if (officer is null || !await _userManager.IsInRoleAsync(officer, RoleNames.ComplaintOfficer) || !officer.IsActive)
        {
            TempData["ErrorMessage"] = "The selected officer is not a valid, active complaint officer.";
            return RedirectToPage(new { id });
        }

        await _complaintService.AssignComplaintAsync(id, officerId, user.Id, user.FullName, notes);
        TempData["SuccessMessage"] = "Complaint assigned successfully.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostChangeStatusAsync(int id, ComplaintStatus status, string? notes)
    {
        if (await LoadComplaintAsync(id) is null) return NotFound();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Forbid();

        await _complaintService.ChangeStatusAsync(id, status, user.Id, user.FullName, notes);
        TempData["SuccessMessage"] = "Complaint status updated.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostChangePriorityAsync(int id, ComplaintPriority priority)
    {
        if (await LoadComplaintAsync(id) is null) return NotFound();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Forbid();

        await _complaintService.ChangePriorityAsync(id, priority, user.Id, user.FullName);
        TempData["SuccessMessage"] = "Complaint priority updated.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostEscalateAsync(int id, string reason)
    {
        if (await LoadComplaintAsync(id) is null) return NotFound();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Forbid();

        await _complaintService.EscalateAsync(id, user.Id, user.FullName, reason);
        TempData["SuccessMessage"] = "Complaint has been escalated.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostResolveAsync(int id, string resolutionSummary)
    {
        if (await LoadComplaintAsync(id) is null) return NotFound();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Forbid();

        await _complaintService.ResolveAsync(id, user.Id, user.FullName, resolutionSummary);
        TempData["SuccessMessage"] = "Complaint marked as resolved.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCloseAsync(int id, string? notes)
    {
        if (await LoadComplaintAsync(id) is null) return NotFound();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Forbid();

        await _complaintService.CloseAsync(id, user.Id, user.FullName, notes);
        TempData["SuccessMessage"] = "Complaint has been closed.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, string reason)
    {
        if (await LoadComplaintAsync(id) is null) return NotFound();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Forbid();

        await _complaintService.RejectAsync(id, user.Id, user.FullName, reason);
        TempData["SuccessMessage"] = "Complaint has been rejected.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReplyAsync(int id, string message, bool isInformationRequest, IFormFile? uploadFile)
    {
        if (await LoadComplaintAsync(id) is null) return NotFound();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Forbid();

        var files = uploadFile is not null ? new List<IFormFile> { uploadFile } : new List<IFormFile>();
        await _complaintService.AddResponseAsync(id, user.Id, user.FullName, ResponseAuthorType.ComplaintOfficer, message, isInformationRequest, files);

        TempData["SuccessMessage"] = "Message sent.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUploadAttachmentAsync(int id, IFormFile uploadFile)
    {
        if (await LoadComplaintAsync(id) is null) return NotFound();
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

    private async Task<Complaint?> LoadComplaintAsync(int id)
    {
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
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    private async Task<ComplaintDetailsViewModel> BuildViewModelAsync(Complaint complaint)
    {
        var officers = await _context.Users
            .Where(u => u.DepartmentId == complaint.DepartmentId && u.IsActive &&
                _context.UserRoles.Any(ur => ur.UserId == u.Id &&
                    _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == RoleNames.ComplaintOfficer)))
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return new ComplaintDetailsViewModel
        {
            Complaint = complaint,
            CanAssign = true,
            CanChangeStatus = true,
            CanChangePriority = true,
            CanEscalate = true,
            CanResolve = true,
            CanClose = true,
            CanReject = true,
            CanReply = true,
            CanRequestInfo = true,
            CanUploadAttachment = true,
            ShowContactInfo = true,
            AvailableOfficers = officers.Select(o => (o.Id, o.FullName)).ToList()
        };
    }
}
