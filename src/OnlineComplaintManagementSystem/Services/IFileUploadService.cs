namespace OnlineComplaintManagementSystem.Services;

public class FileUploadResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StoredPath { get; set; }
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long FileSizeBytes { get; set; }
}

public interface IFileUploadService
{
    Task<FileUploadResult> SaveComplaintAttachmentAsync(IFormFile file, int complaintId);
    void DeleteFile(string storedPath);
    string GetFullPath(string storedPath);

    /// <summary>Saves a profile photo under wwwroot and returns a web-relative URL path (e.g. "/uploads/avatars/xxxx.jpg").</summary>
    Task<FileUploadResult> SaveProfilePhotoAsync(IFormFile file, string userId);

    /// <summary>Deletes a profile photo given the web-relative path returned by SaveProfilePhotoAsync.</summary>
    void DeleteProfilePhoto(string webRelativePath);
}
