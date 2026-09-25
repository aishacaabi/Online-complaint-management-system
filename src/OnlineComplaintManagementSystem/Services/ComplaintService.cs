using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.Services;

public class ComplaintService : IComplaintService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly IFileUploadService _fileUploadService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ComplaintService(
        ApplicationDbContext context,
        INotificationService notificationService,
        IAuditLogService auditLogService,
        IFileUploadService fileUploadService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
        _fileUploadService = fileUploadService;
        _userManager = userManager;
    }

    public async Task<Complaint> SubmitComplaintAsync(Complaint complaint, List<IFormFile> files, string submittedById, string submittedByName)
    {
        var defaultResolutionDays = await _context.SystemSettings.Select(s => s.DefaultResolutionDays).FirstOrDefaultAsync();
        if (defaultResolutionDays <= 0) defaultResolutionDays = 14;

        complaint.ComplainantId = submittedById;
        complaint.Status = ComplaintStatus.Submitted;
        complaint.SubmittedDate = DateTime.UtcNow;
        complaint.ExpectedResolutionDate = DateTime.UtcNow.AddDays(defaultResolutionDays);

        _context.Complaints.Add(complaint);
        await _context.SaveChangesAsync();

        complaint.ReferenceNumber = $"CMP-{complaint.SubmittedDate:yyyy}-{complaint.Id:D6}";

        _context.ComplaintStatusHistories.Add(new ComplaintStatusHistory
        {
            ComplaintId = complaint.Id,
            PreviousStatus = null,
            NewStatus = ComplaintStatus.Submitted,
            ChangedById = submittedById,
            Notes = "Complaint submitted.",
            ChangedDate = complaint.SubmittedDate
        });

        await _context.SaveChangesAsync();

        foreach (var file in files.Where(f => f.Length > 0))
        {
            var result = await _fileUploadService.SaveComplaintAttachmentAsync(file, complaint.Id);
            if (result.Success)
            {
                _context.ComplaintAttachments.Add(new ComplaintAttachment
                {
                    ComplaintId = complaint.Id,
                    FileName = result.OriginalFileName!,
                    StoredPath = result.StoredPath!,
                    ContentType = result.ContentType!,
                    FileSizeBytes = result.FileSizeBytes,
                    UploadedById = submittedById,
                    UploadedDate = DateTime.UtcNow
                });
            }
        }
        await _context.SaveChangesAsync();

        var admins = await _userManager.GetUsersInRoleAsync(RoleNames.SuperAdministrator);
        foreach (var admin in admins)
        {
            await _notificationService.CreateAsync(admin.Id, NotificationType.ComplaintSubmitted,
                "New Complaint Submitted",
                $"Complaint {complaint.ReferenceNumber} \"{complaint.Title}\" was submitted and requires review.",
                complaint.Id);
        }

        await _auditLogService.LogAsync(submittedById, submittedByName, "Complaint Submitted",
            $"Complaint {complaint.ReferenceNumber} submitted.");

        return complaint;
    }

    public async Task AssignComplaintAsync(int complaintId, string officerId, string assignedById, string assignedByName, string? notes)
    {
        var complaint = await _context.Complaints.FirstAsync(c => c.Id == complaintId);

        var openAssignments = await _context.ComplaintAssignments
            .Where(a => a.ComplaintId == complaintId && a.UnassignedDate == null)
            .ToListAsync();
        foreach (var a in openAssignments)
        {
            a.UnassignedDate = DateTime.UtcNow;
        }

        _context.ComplaintAssignments.Add(new ComplaintAssignment
        {
            ComplaintId = complaintId,
            OfficerId = officerId,
            AssignedById = assignedById,
            AssignedDate = DateTime.UtcNow,
            Notes = notes
        });

        var previousStatus = complaint.Status;
        complaint.Status = ComplaintStatus.Assigned;
        complaint.UpdatedDate = DateTime.UtcNow;

        _context.ComplaintStatusHistories.Add(new ComplaintStatusHistory
        {
            ComplaintId = complaintId,
            PreviousStatus = previousStatus,
            NewStatus = ComplaintStatus.Assigned,
            ChangedById = assignedById,
            Notes = notes ?? "Complaint assigned to officer.",
            ChangedDate = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        await _notificationService.CreateAsync(officerId, NotificationType.ComplaintAssigned,
            "Complaint Assigned to You",
            $"Complaint {complaint.ReferenceNumber} \"{complaint.Title}\" has been assigned to you.",
            complaintId);

        await _notificationService.CreateAsync(complaint.ComplainantId, NotificationType.ComplaintAssigned,
            "Your Complaint Was Assigned",
            $"Complaint {complaint.ReferenceNumber} has been assigned to a complaint officer.",
            complaintId);

        await _auditLogService.LogAsync(assignedById, assignedByName, "Complaint Assigned",
            $"Complaint {complaint.ReferenceNumber} assigned to officer {officerId}.");
    }

    public async Task ChangeStatusAsync(int complaintId, ComplaintStatus newStatus, string changedById, string changedByName, string? notes)
    {
        var complaint = await _context.Complaints.FirstAsync(c => c.Id == complaintId);
        var previousStatus = complaint.Status;

        complaint.Status = newStatus;
        complaint.UpdatedDate = DateTime.UtcNow;

        _context.ComplaintStatusHistories.Add(new ComplaintStatusHistory
        {
            ComplaintId = complaintId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedById = changedById,
            Notes = notes,
            ChangedDate = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        await _notificationService.CreateAsync(complaint.ComplainantId, NotificationType.StatusChanged,
            "Complaint Status Updated",
            $"Complaint {complaint.ReferenceNumber} status changed to \"{newStatus}\".",
            complaintId);

        await _auditLogService.LogAsync(changedById, changedByName, "Complaint Status Changed",
            $"Complaint {complaint.ReferenceNumber} status changed from {previousStatus} to {newStatus}.");
    }

    public async Task ChangePriorityAsync(int complaintId, ComplaintPriority priority, string changedById, string changedByName)
    {
        var complaint = await _context.Complaints.FirstAsync(c => c.Id == complaintId);
        var previous = complaint.Priority;
        complaint.Priority = priority;
        complaint.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(changedById, changedByName, "Complaint Priority Changed",
            $"Complaint {complaint.ReferenceNumber} priority changed from {previous} to {priority}.");
    }

    public async Task<ComplaintResponse> AddResponseAsync(int complaintId, string authorId, string authorName, ResponseAuthorType authorType, string message, bool isInformationRequest, List<IFormFile> files)
    {
        // Assignments are needed to find the current officer when notifying about a complainant's reply.
        var complaint = await _context.Complaints
            .Include(c => c.Assignments).ThenInclude(a => a.Officer)
            .FirstAsync(c => c.Id == complaintId);

        var response = new ComplaintResponse
        {
            ComplaintId = complaintId,
            AuthorId = authorId,
            AuthorType = authorType,
            Message = message,
            IsInformationRequest = isInformationRequest,
            CreatedDate = DateTime.UtcNow
        };
        _context.ComplaintResponses.Add(response);
        await _context.SaveChangesAsync();

        foreach (var file in files.Where(f => f.Length > 0))
        {
            var result = await _fileUploadService.SaveComplaintAttachmentAsync(file, complaintId);
            if (result.Success)
            {
                _context.ComplaintAttachments.Add(new ComplaintAttachment
                {
                    ComplaintId = complaintId,
                    ComplaintResponseId = response.Id,
                    FileName = result.OriginalFileName!,
                    StoredPath = result.StoredPath!,
                    ContentType = result.ContentType!,
                    FileSizeBytes = result.FileSizeBytes,
                    UploadedById = authorId,
                    UploadedDate = DateTime.UtcNow
                });
            }
        }

        if (isInformationRequest && complaint.Status != ComplaintStatus.PendingInformation)
        {
            var previousStatus = complaint.Status;
            complaint.Status = ComplaintStatus.PendingInformation;
            _context.ComplaintStatusHistories.Add(new ComplaintStatusHistory
            {
                ComplaintId = complaintId,
                PreviousStatus = previousStatus,
                NewStatus = ComplaintStatus.PendingInformation,
                ChangedById = authorId,
                Notes = "Additional information requested.",
                ChangedDate = DateTime.UtcNow
            });
        }
        complaint.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var recipientId = authorType == ResponseAuthorType.Complainant
            ? (complaint.CurrentOfficer?.Id ?? complaint.ComplainantId)
            : complaint.ComplainantId;

        if (!string.IsNullOrEmpty(recipientId) && recipientId != authorId)
        {
            var notifType = isInformationRequest ? NotificationType.InformationRequested : NotificationType.NewResponse;
            await _notificationService.CreateAsync(recipientId, notifType,
                isInformationRequest ? "Additional Information Requested" : "New Message on Your Complaint",
                $"A new message was posted on complaint {complaint.ReferenceNumber}.",
                complaintId);
        }

        await _auditLogService.LogAsync(authorId, authorName, "Complaint Response Added",
            $"Response added to complaint {complaint.ReferenceNumber}.");

        return response;
    }

    public async Task EscalateAsync(int complaintId, string byId, string byName, string reason)
    {
        var complaint = await _context.Complaints.FirstAsync(c => c.Id == complaintId);
        var previous = complaint.Status;
        complaint.Status = ComplaintStatus.Escalated;
        complaint.UpdatedDate = DateTime.UtcNow;

        _context.ComplaintStatusHistories.Add(new ComplaintStatusHistory
        {
            ComplaintId = complaintId,
            PreviousStatus = previous,
            NewStatus = ComplaintStatus.Escalated,
            ChangedById = byId,
            Notes = reason,
            ChangedDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var admins = await _userManager.GetUsersInRoleAsync(RoleNames.SuperAdministrator);
        foreach (var admin in admins)
        {
            await _notificationService.CreateAsync(admin.Id, NotificationType.ComplaintEscalated,
                "Complaint Escalated",
                $"Complaint {complaint.ReferenceNumber} has been escalated: {reason}",
                complaintId);
        }
        await _notificationService.CreateAsync(complaint.ComplainantId, NotificationType.ComplaintEscalated,
            "Your Complaint Was Escalated",
            $"Complaint {complaint.ReferenceNumber} has been escalated for further review.",
            complaintId);

        await _auditLogService.LogAsync(byId, byName, "Complaint Escalated",
            $"Complaint {complaint.ReferenceNumber} escalated. Reason: {reason}");
    }

    public async Task ResolveAsync(int complaintId, string byId, string byName, string resolutionSummary)
    {
        var complaint = await _context.Complaints.FirstAsync(c => c.Id == complaintId);
        var previous = complaint.Status;
        complaint.Status = ComplaintStatus.Resolved;
        complaint.ResolutionSummary = resolutionSummary;
        complaint.ResolvedDate = DateTime.UtcNow;
        complaint.UpdatedDate = DateTime.UtcNow;

        _context.ComplaintStatusHistories.Add(new ComplaintStatusHistory
        {
            ComplaintId = complaintId,
            PreviousStatus = previous,
            NewStatus = ComplaintStatus.Resolved,
            ChangedById = byId,
            Notes = resolutionSummary,
            ChangedDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await _notificationService.CreateAsync(complaint.ComplainantId, NotificationType.ComplaintResolved,
            "Complaint Resolved",
            $"Complaint {complaint.ReferenceNumber} has been resolved. Please review and provide feedback.",
            complaintId);

        await _auditLogService.LogAsync(byId, byName, "Complaint Resolved",
            $"Complaint {complaint.ReferenceNumber} marked resolved.");
    }

    public async Task CloseAsync(int complaintId, string byId, string byName, string? notes)
    {
        var complaint = await _context.Complaints.FirstAsync(c => c.Id == complaintId);
        var previous = complaint.Status;
        complaint.Status = ComplaintStatus.Closed;
        complaint.ClosedDate = DateTime.UtcNow;
        complaint.UpdatedDate = DateTime.UtcNow;

        _context.ComplaintStatusHistories.Add(new ComplaintStatusHistory
        {
            ComplaintId = complaintId,
            PreviousStatus = previous,
            NewStatus = ComplaintStatus.Closed,
            ChangedById = byId,
            Notes = notes,
            ChangedDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await _notificationService.CreateAsync(complaint.ComplainantId, NotificationType.ComplaintClosed,
            "Complaint Closed",
            $"Complaint {complaint.ReferenceNumber} has been closed.",
            complaintId);

        await _auditLogService.LogAsync(byId, byName, "Complaint Closed",
            $"Complaint {complaint.ReferenceNumber} closed.");
    }

    public async Task RejectAsync(int complaintId, string byId, string byName, string reason)
    {
        var complaint = await _context.Complaints.FirstAsync(c => c.Id == complaintId);
        var previous = complaint.Status;
        complaint.Status = ComplaintStatus.Rejected;
        complaint.UpdatedDate = DateTime.UtcNow;

        _context.ComplaintStatusHistories.Add(new ComplaintStatusHistory
        {
            ComplaintId = complaintId,
            PreviousStatus = previous,
            NewStatus = ComplaintStatus.Rejected,
            ChangedById = byId,
            Notes = reason,
            ChangedDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await _notificationService.CreateAsync(complaint.ComplainantId, NotificationType.StatusChanged,
            "Complaint Rejected",
            $"Complaint {complaint.ReferenceNumber} was rejected. Reason: {reason}",
            complaintId);

        await _auditLogService.LogAsync(byId, byName, "Complaint Rejected",
            $"Complaint {complaint.ReferenceNumber} rejected. Reason: {reason}");
    }
}
