using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.Pages.Complainant.Complaints;

public class ConfirmationModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ConfirmationModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Complaint? Complaint { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        Complaint = await _context.Complaints.FirstOrDefaultAsync(c => c.Id == id && c.ComplainantId == userId);
        if (Complaint is null)
        {
            return NotFound();
        }
        return Page();
    }
}
