using Microsoft.AspNetCore.Http;
using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.Services;

public interface IComplaintService
{
    Task<Complaint> SubmitComplaintAsync(Complaint complaint, List<IFormFile> files, string submittedById, string submittedByName);
    Task AssignComplaintAsync(int complaintId, string officerId, string assignedById, string assignedByName, string? notes);
    Task ChangeStatusAsync(int complaintId, ComplaintStatus newStatus, string changedById, string changedByName, string? notes);
    Task ChangePriorityAsync(int complaintId, ComplaintPriority priority, string changedById, string changedByName);
    Task<ComplaintResponse> AddResponseAsync(int complaintId, string authorId, string authorName, ResponseAuthorType authorType, string message, bool isInformationRequest, List<IFormFile> files);
    Task EscalateAsync(int complaintId, string byId, string byName, string reason);
    Task ResolveAsync(int complaintId, string byId, string byName, string resolutionSummary);
    Task CloseAsync(int complaintId, string byId, string byName, string? notes);
    Task RejectAsync(int complaintId, string byId, string byName, string reason);
}
