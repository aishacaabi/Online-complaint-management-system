using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.Pages.Complaints;

public class TrackModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public TrackModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public Complaint? Result { get; set; }
    public bool Searched { get; set; }

    public class InputModel
    {
        [Required, Display(Name = "Complaint Reference Number")]
        public string ReferenceNumber { get; set; } = string.Empty;

        [Required, EmailAddress, Display(Name = "Email Used When Submitting")]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Searched = true;
        var refNumber = Input.ReferenceNumber.Trim();
        var email = Input.Email.Trim();

        Result = await _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.Department)
            .Include(c => c.Assignments).ThenInclude(a => a.Officer)
            .Include(c => c.StatusHistory).ThenInclude(h => h.ChangedBy)
            .Include(c => c.Complainant)
            .Where(c => c.ReferenceNumber == refNumber &&
                        ((c.ContactEmail != null && c.ContactEmail == email) || c.Complainant!.Email == email))
            .FirstOrDefaultAsync();

        if (Result is null)
        {
            ModelState.AddModelError(string.Empty, "No complaint was found matching that reference number and email address.");
        }

        return Page();
    }
}
