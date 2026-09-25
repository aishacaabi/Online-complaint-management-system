using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;
using OnlineComplaintManagementSystem.Services;
using OnlineComplaintManagementSystem.ViewModels;

namespace OnlineComplaintManagementSystem.Pages.Complainant.Complaints;

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
        var complaint = await LoadOwnedComplaintAsync(id);
        if (complaint is null) return NotFound();

        BuildViewModel(complaint);
        return Page();
    }

    public async Task<IActionResult> OnPostReplyAsync(int id, string message, IFormFile? uploadFile)
    {
        var complaint = await LoadOwnedComplaintAsync(id);
        if (complaint is null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Forbid();

        var files = uploadFile is not null ? new List<IFormFile> { uploadFile } : new List<IFormFile>();
        await _complaintService.AddResponseAsync(id, user.Id, user.FullName, ResponseAuthorType.Complainant, message, false, files);

        TempData["SuccessMessage"] = "Your message has been sent.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUploadAttachmentAsync(int id, IFormFile uploadFile)
    {
        var complaint = await LoadOwnedComplaintAsync(id);
        if (complaint is null) return NotFound();

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

    public async Task<IActionResult> OnPostFeedbackAsync(int id, int rating, string? comments)
    {
        var complaint = await LoadOwnedComplaintAsync(id);
        if (complaint is null) return NotFound();

        if (complaint.Feedback is not null || (complaint.Status != ComplaintStatus.Resolved && complaint.Status != ComplaintStatus.Closed))
        {
            return RedirectToPage(new { id });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Forbid();

        _context.Feedbacks.Add(new Feedback
        {
            ComplaintId = id,
            SubmittedById = user.Id,
            Rating = Math.Clamp(rating, 1, 5),
            Comments = comments,
            SubmittedDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thank you for your feedback.";
        return RedirectToPage(new { id });
    }

    private async Task<Complaint?> LoadOwnedComplaintAsync(int id)
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
            .FirstOrDefaultAsync(c => c.Id == id && c.ComplainantId == userId);
    }

    private void BuildViewModel(Complaint complaint)
    {
        Vm = new ComplaintDetailsViewModel
        {
            Complaint = complaint,
            CanReply = true,
            CanUploadAttachment = true,
            CanGiveFeedback = true,
            ShowContactInfo = false
        };
    }
}
