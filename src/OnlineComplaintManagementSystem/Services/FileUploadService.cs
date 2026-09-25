namespace OnlineComplaintManagementSystem.Services;

public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _environment;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt"
    };

    private static readonly Dictionary<string, string[]> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = new[] { "image/jpeg" },
        [".jpeg"] = new[] { "image/jpeg" },
        [".png"] = new[] { "image/png" },
        [".gif"] = new[] { "image/gif" },
        [".pdf"] = new[] { "application/pdf" },
        [".doc"] = new[] { "application/msword" },
        [".docx"] = new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        [".xls"] = new[] { "application/vnd.ms-excel" },
        [".xlsx"] = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        [".txt"] = new[] { "text/plain" }
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif"
    };

    private const long MaxPhotoSizeBytes = 3 * 1024 * 1024; // 3 MB

    public FileUploadService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>Root storage location kept outside wwwroot so files can only be reached through an authorized page handler.</summary>
    private string UploadsRoot => Path.Combine(_environment.ContentRootPath, "Uploads", "Complaints");

    /// <summary>Avatars are non-sensitive, so they're stored inside wwwroot and served directly by static files.</summary>
    private string AvatarsRoot => Path.Combine(_environment.WebRootPath, "uploads", "avatars");

    public async Task<FileUploadResult> SaveComplaintAttachmentAsync(IFormFile file, int complaintId)
    {
        if (file is null || file.Length == 0)
        {
            return new FileUploadResult { Success = false, ErrorMessage = "No file was provided." };
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return new FileUploadResult { Success = false, ErrorMessage = "File exceeds the maximum allowed size of 10 MB." };
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            return new FileUploadResult { Success = false, ErrorMessage = "File type is not permitted." };
        }

        if (AllowedContentTypes.TryGetValue(extension, out var validTypes) &&
            !validTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return new FileUploadResult { Success = false, ErrorMessage = "The file content does not match its extension." };
        }

        var folder = Path.Combine(UploadsRoot, complaintId.ToString());
        Directory.CreateDirectory(folder);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, storedFileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return new FileUploadResult
        {
            Success = true,
            StoredPath = Path.Combine(complaintId.ToString(), storedFileName),
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType,
            FileSizeBytes = file.Length
        };
    }

    public void DeleteFile(string storedPath)
    {
        var fullPath = Path.Combine(UploadsRoot, storedPath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    public string GetFullPath(string storedPath) => Path.Combine(UploadsRoot, storedPath);

    public async Task<FileUploadResult> SaveProfilePhotoAsync(IFormFile file, string userId)
    {
        if (file is null || file.Length == 0)
        {
            return new FileUploadResult { Success = false, ErrorMessage = "No file was provided." };
        }

        if (file.Length > MaxPhotoSizeBytes)
        {
            return new FileUploadResult { Success = false, ErrorMessage = "Photo exceeds the maximum allowed size of 3 MB." };
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
        {
            return new FileUploadResult { Success = false, ErrorMessage = "Only JPG, PNG, and GIF images are permitted." };
        }

        if (AllowedContentTypes.TryGetValue(extension, out var validTypes) &&
            !validTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return new FileUploadResult { Success = false, ErrorMessage = "The file content does not match its extension." };
        }

        Directory.CreateDirectory(AvatarsRoot);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(AvatarsRoot, storedFileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return new FileUploadResult
        {
            Success = true,
            StoredPath = $"/uploads/avatars/{storedFileName}",
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType,
            FileSizeBytes = file.Length
        };
    }

    public void DeleteProfilePhoto(string webRelativePath)
    {
        var fileName = Path.GetFileName(webRelativePath);
        var fullPath = Path.Combine(AvatarsRoot, fileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
