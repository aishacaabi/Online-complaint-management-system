using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;
using OnlineComplaintManagementSystem.Services;

namespace OnlineComplaintManagementSystem.Pages.Complainant.Complaints;

public class SubmitModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IComplaintService _complaintService;

    public SubmitModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IComplaintService complaintService)
    {
        _context = context;
        _userManager = userManager;
        _complaintService = complaintService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<ComplaintCategory> Categories { get; set; } = new();
    public List<Department> Departments { get; set; } = new();

    public class InputModel
    {
        [Required, StringLength(200)]
        [Display(Name = "Complaint Title")]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Required, Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required, Display(Name = "Department")]
        public int DepartmentId { get; set; }

        [Required]
        public ComplaintPriority Priority { get; set; } = ComplaintPriority.Medium;

        [StringLength(250)]
        public string? Location { get; set; }

        [Display(Name = "Contact Name")]
        [StringLength(150)]
        public string? ContactName { get; set; }

        [Display(Name = "Contact Phone")]
        [Phone]
        public string? ContactPhone { get; set; }

        [Display(Name = "Contact Email")]
        [EmailAddress]
        public string? ContactEmail { get; set; }

        public List<IFormFile> Attachments { get; set; } = new();
    }

    public async Task OnGetAsync()
    {
        await LoadDropdownsAsync();
        var user = await _userManager.GetUserAsync(User);
        if (user is not null)
        {
            Input.ContactName = user.FullName;
            Input.ContactPhone = user.PhoneNumber;
            Input.ContactEmail = user.Email;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await _context.Departments.AnyAsync(d => d.Id == Input.DepartmentId && d.IsActive))
        {
            ModelState.AddModelError("Input.DepartmentId", "Please select an active department.");
        }

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var complaint = new Complaint
        {
            Title = Input.Title,
            Description = Input.Description,
            CategoryId = Input.CategoryId,
            DepartmentId = Input.DepartmentId,
            Priority = Input.Priority,
            Location = Input.Location,
            ContactName = Input.ContactName,
            ContactPhone = Input.ContactPhone,
            ContactEmail = Input.ContactEmail
        };

        var created = await _complaintService.SubmitComplaintAsync(complaint, Input.Attachments, user.Id, user.FullName);

        return RedirectToPage("/Complainant/Complaints/Confirmation", new { id = created.Id });
    }

    private async Task LoadDropdownsAsync()
    {
        Categories = await _context.ComplaintCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
        Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
    }
}
