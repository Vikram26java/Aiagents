using BusinessAiAgent.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BusinessAiAgent.Web.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IWebHostEnvironment _env;

    public SettingsController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _env = env;
    }

    public async Task<IActionResult> Index(string? tab)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        ViewBag.ActiveTab = tab ?? "appearance";
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (newPassword != confirmPassword)
        {
            TempData["Error"] = "New passwords do not match.";
            return RedirectToAction("Index", new { tab = "account" });
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Password changed successfully.";
        }
        else
        {
            TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction("Index", new { tab = "account" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile avatar)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (avatar == null || avatar.Length == 0)
        {
            TempData["Error"] = "Please select an image.";
            return RedirectToAction("Index", new { tab = "account" });
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(avatar.ContentType))
        {
            TempData["Error"] = "Only JPEG, PNG, GIF, and WebP images are allowed.";
            return RedirectToAction("Index", new { tab = "account" });
        }

        if (avatar.Length > 5 * 1024 * 1024)
        {
            TempData["Error"] = "Image must be less than 5MB.";
            return RedirectToAction("Index", new { tab = "account" });
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(uploadsDir);

        var extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension)) extension = ".jpg";
        var fileName = $"{user.Id}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);

        // Delete old avatar if different extension
        var existingFiles = Directory.GetFiles(uploadsDir, $"{user.Id}.*");
        foreach (var f in existingFiles)
        {
            System.IO.File.Delete(f);
        }

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await avatar.CopyToAsync(stream);
        }

        user.ProfileImageUrl = $"/uploads/avatars/{fileName}";
        await _userManager.UpdateAsync(user);

        TempData["Success"] = "Avatar updated successfully.";
        return RedirectToAction("Index", new { tab = "account" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAvatar()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (!string.IsNullOrEmpty(user.ProfileImageUrl))
        {
            var filePath = Path.Combine(_env.WebRootPath, user.ProfileImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            user.ProfileImageUrl = null;
            await _userManager.UpdateAsync(user);
        }

        TempData["Success"] = "Avatar removed.";
        return RedirectToAction("Index", new { tab = "account" });
    }
}
