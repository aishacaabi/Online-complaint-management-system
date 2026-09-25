using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;
using OnlineComplaintManagementSystem.Services;

namespace OnlineComplaintManagementSystem.Pages.Complaints;

[Authorize]
public class DownloadAttachmentModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _fileUploadService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DownloadAttachmentModel(ApplicationDbContext context, IFileUploadService fileUploadService, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _fileUploadService = fileUploadService;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var attachment = await _context.ComplaintAttachments
            .Include(a => a.Complaint)
            .ThenInclude(c => c!.Assignments)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attachment?.Complaint is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Forbid();
        }

        var isAdmin = User.IsInRole(RoleNames.SuperAdministrator);
        var isOwner = attachment.Complaint.ComplainantId == user.Id;
        var isAssignedOfficer = attachment.Complaint.Assignments.Any(a => a.OfficerId == user.Id && a.UnassignedDate == null);

        if (!isAdmin && !isOwner && !isAssignedOfficer)
        {
            return Forbid();
        }

        var fullPath = _fileUploadService.GetFullPath(attachment.StoredPath);
        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(attachment.FileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        return File(bytes, contentType, attachment.FileName);
    }
}
