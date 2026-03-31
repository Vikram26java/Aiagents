using BusinessAiAgent.Core.Entities;
using BusinessAiAgent.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BusinessAiAgent.Web.Controllers;

[Authorize(Roles = "Customer")]
public class CustomerController : Controller
{
    private readonly IConversationService _conversationService;
    private readonly IContactService _contactService;
    private readonly ICompanyService _companyService;
    private readonly UserManager<AppUser> _userManager;

    public CustomerController(
        IConversationService conversationService,
        IContactService contactService,
        ICompanyService companyService,
        UserManager<AppUser> userManager)
    {
        _conversationService = conversationService;
        _contactService = contactService;
        _companyService = companyService;
        _userManager = userManager;
    }

    private async Task<AppUser> GetCurrentUserAsync()
    {
        return (await _userManager.GetUserAsync(User))!;
    }

    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentUserAsync();
        var conversations = user.ContactId.HasValue
            ? await _conversationService.GetConversationsForContactAsync(user.ContactId.Value)
            : [];

        var totalMessages = conversations.Sum(c => c.Messages.Count);
        ViewBag.TotalConversations = conversations.Count;
        ViewBag.TotalMessages = totalMessages;
        ViewBag.RecentConversations = conversations.Take(10).ToList();
        return View();
    }

    public async Task<IActionResult> Conversations()
    {
        var user = await GetCurrentUserAsync();
        var conversations = user.ContactId.HasValue
            ? await _conversationService.GetConversationsForContactAsync(user.ContactId.Value)
            : [];
        return View(conversations);
    }

    public async Task<IActionResult> ConversationDetail(int id)
    {
        var user = await GetCurrentUserAsync();
        var conversation = await _conversationService.GetConversationAsync(id);
        if (conversation == null) return NotFound();

        // Ensure customer can only see own conversations
        if (conversation.ContactId != user.ContactId)
            return Forbid();

        return View(conversation);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await GetCurrentUserAsync();
        Contact? contact = user.ContactId.HasValue
            ? await _contactService.GetContactAsync(user.ContactId.Value)
            : null;
        ViewBag.Contact = contact;
        ViewBag.Companies = await _companyService.GetAllAsync();
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(string fullName, string? phone, string? address, int? companyId)
    {
        var user = await GetCurrentUserAsync();
        user.FullName = fullName;
        user.PhoneNumber = phone;
        user.Address = address;
        user.CompanyId = companyId;
        await _userManager.UpdateAsync(user);

        if (user.ContactId.HasValue)
        {
            var contact = await _contactService.GetContactAsync(user.ContactId.Value);
            if (contact != null)
                await _contactService.UpdateContactAsync(contact.Id, fullName, contact.Email, phone, companyId);
        }

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction("Profile");
    }

    // Calendar placeholder
    public IActionResult Calendar() => View();

    // Direct Chat placeholder
    public IActionResult Chat() => View();
}
