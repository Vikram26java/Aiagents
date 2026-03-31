using BusinessAiAgent.Core.DTOs;
using BusinessAiAgent.Core.Entities;
using BusinessAiAgent.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BusinessAiAgent.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IContactService _contactService;
    private readonly ICompanyService _companyService;

    public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IContactService contactService,
        ICompanyService companyService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _contactService = contactService;
        _companyService = companyService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToRoleDashboard();
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                    return Redirect(returnUrl ?? "/Admin");
                return Redirect(returnUrl ?? "/Customer");
            }
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Signup()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToRoleDashboard();
        ViewBag.Companies = await _companyService.GetAllAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Signup(SignupViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await _companyService.GetAllAsync();
            return View(model);
        }

        // Create Contact
        var contact = await _contactService.GetOrCreateContactAsync(model.FullName, model.Email, model.Phone);
        if (model.CompanyId.HasValue)
            await _contactService.UpdateContactAsync(contact.Id, contact.Name, contact.Email, contact.Phone, model.CompanyId);

        // Create AppUser
        var user = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.Phone,
            Address = model.Address,
            ContactId = contact.Id,
            CompanyId = model.CompanyId,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Customer");
            await _signInManager.SignInAsync(user, isPersistent: false);
            return Redirect("/Customer");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        ViewBag.Companies = await _companyService.GetAllAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToRoleDashboard()
    {
        if (User.IsInRole("Admin")) return Redirect("/Admin");
        return Redirect("/Customer");
    }
}
