using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;
using OnlineComplaintManagementSystem.Services;

namespace OnlineComplaintManagementSystem.Pages.Admin.Categories;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLogService;

    public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditLogService auditLogService)
    {
        _context = context;
        _userManager = userManager;
        _auditLogService = auditLogService;
    }

    public List<ComplaintCategory> Categories { get; set; } = new();
    public Dictionary<int, int> ComplaintCounts { get; set; } = new();

    [BindProperty]
    public CategoryInput Input { get; set; } = new();

    public class CategoryInput
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            TempData["ErrorMessage"] = "Please correct the errors and try again.";
            return Page();
        }

        // Category names are unique in the database; report a clash instead of failing with a SQL error.
        var name = Input.Name.Trim();
        if (await _context.ComplaintCategories.AnyAsync(c => c.Name == name && c.Id != Input.Id))
        {
            TempData["ErrorMessage"] = $"A category named \"{name}\" already exists.";
            return RedirectToPage();
        }
        Input.Name = name;

        var admin = await _userManager.GetUserAsync(User);

        if (Input.Id == 0)
        {
            var cat = new ComplaintCategory
            {
                Name = Input.Name,
                Description = Input.Description,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            _context.ComplaintCategories.Add(cat);
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(admin?.Id, admin?.FullName ?? "Admin", "Category Created", $"Category \"{cat.Name}\" created.");
            TempData["SuccessMessage"] = "Category created successfully.";
        }
        else
        {
            var cat = await _context.ComplaintCategories.FindAsync(Input.Id);
            if (cat is null) return NotFound();

            cat.Name = Input.Name;
            cat.Description = Input.Description;
            cat.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(admin?.Id, admin?.FullName ?? "Admin", "Category Updated", $"Category \"{cat.Name}\" updated.");
            TempData["SuccessMessage"] = "Category updated successfully.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var cat = await _context.ComplaintCategories.FindAsync(id);
        if (cat is null) return NotFound();

        // Complaints reference the category with a restrict-on-delete foreign key, so check first
        // and explain the problem instead of throwing a SQL error.
        var complaintCount = await _context.Complaints.CountAsync(c => c.CategoryId == id);
        if (complaintCount > 0)
        {
            TempData["ErrorMessage"] = $"\"{cat.Name}\" cannot be deleted because {complaintCount} complaint(s) use it. Recategorise or remove those complaints first.";
            return RedirectToPage();
        }

        var name = cat.Name;
        _context.ComplaintCategories.Remove(cat);
        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        await _auditLogService.LogAsync(admin?.Id, admin?.FullName ?? "Admin",
            "Category Deleted", $"Category \"{name}\" permanently deleted.");

        TempData["SuccessMessage"] = $"Category \"{name}\" was permanently deleted.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Categories = await _context.ComplaintCategories.OrderBy(c => c.Name).ToListAsync();
        ComplaintCounts = await _context.Complaints
            .GroupBy(c => c.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);
    }
}
