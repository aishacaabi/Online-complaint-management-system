using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OnlineComplaintManagementSystem.Pages.Account;

public class ForgotPasswordConfirmationModel : PageModel
{
    [TempData]
    public string? SimulatedResetLink { get; set; }

    public void OnGet()
    {
    }
}
