using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.Pages;

public class IndexModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (_signInManager.IsSignedIn(User))
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is not null)
            {
                if (await _userManager.IsInRoleAsync(user, RoleNames.SuperAdministrator))
                {
                    return RedirectToPage("/Admin/Dashboard");
                }
                if (await _userManager.IsInRoleAsync(user, RoleNames.ComplaintOfficer))
                {
                    return RedirectToPage("/Officer/Dashboard");
                }
                return RedirectToPage("/Complainant/Dashboard");
            }
        }
        return Page();
    }
}
