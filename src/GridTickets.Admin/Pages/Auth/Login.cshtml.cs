using System.ComponentModel.DataAnnotations;
using GridTickets.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridTickets.Admin.Pages.Auth;

public class LoginInputModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class LoginModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    [BindProperty]
    public LoginInputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public LoginModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        TempData["ReturnUrl"] = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, Input.Password))
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains("Admin"))
        {
            ErrorMessage = "Access denied. Admin privileges required.";
            return Page();
        }

        await _signInManager.SignInAsync(user, isPersistent: Input.RememberMe);

        var returnUrl = TempData["ReturnUrl"]?.ToString();
        return LocalRedirect(returnUrl ?? "/");
    }
}
