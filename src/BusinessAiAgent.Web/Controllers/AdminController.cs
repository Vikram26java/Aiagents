using BusinessAiAgent.Core.Entities;
using BusinessAiAgent.Core.Enums;
using BusinessAiAgent.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusinessAiAgent.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IConversationService _conversationService;
    private readonly IContactService _contactService;
    private readonly ICompanyService _companyService;
    private readonly UserManager<AppUser> _userManager;

    public AdminController(
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

    public async Task<IActionResult> Index()
    {
        var stats = await _conversationService.GetDashboardStatsAsync();
        var recent = await _conversationService.GetConversationsAsync();
        var companies = await _companyService.GetAllAsync();
        var users = await _userManager.Users.ToListAsync();
        ViewBag.Stats = stats;
        ViewBag.RecentConversations = recent.Take(10).ToList();
        ViewBag.CompanyCount = companies.Count;
        ViewBag.UserCount = users.Count;
        return View();
    }

    // --- Conversations ---
    public async Task<IActionResult> Conversations(string? channel)
    {
        ChannelType? filter = null;
        if (Enum.TryParse<ChannelType>(channel, out var ch))
            filter = ch;
        var conversations = await _conversationService.GetConversationsAsync(filter);
        ViewBag.ChannelFilter = channel;
        return View(conversations);
    }

    public async Task<IActionResult> ConversationDetail(int id)
    {
        var conversation = await _conversationService.GetConversationAsync(id);
        if (conversation == null) return NotFound();
        return View(conversation);
    }

    [HttpGet]
    public async Task<IActionResult> EditConversation(int id)
    {
        var conversation = await _conversationService.GetConversationAsync(id);
        if (conversation == null) return NotFound();
        return View(conversation);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditConversation(int id, string subject)
    {
        await _conversationService.UpdateConversationAsync(id, subject);
        return RedirectToAction("ConversationDetail", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConversation(int id)
    {
        await _conversationService.DeleteConversationAsync(id);
        return RedirectToAction("Conversations");
    }

    // --- Contacts ---
    public async Task<IActionResult> Contacts(string? search)
    {
        var contacts = await _contactService.GetContactsAsync(search);
        ViewBag.Search = search;
        return View(contacts);
    }

    public async Task<IActionResult> ContactDetail(int id)
    {
        var contact = await _contactService.GetContactAsync(id);
        if (contact == null) return NotFound();
        return View(contact);
    }

    [HttpGet]
    public async Task<IActionResult> EditContact(int id)
    {
        var contact = await _contactService.GetContactAsync(id);
        if (contact == null) return NotFound();
        ViewBag.Companies = await _companyService.GetAllAsync();
        return View(contact);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditContact(int id, string name, string? email, string? phone, int? companyId)
    {
        await _contactService.UpdateContactAsync(id, name, email, phone, companyId);
        return RedirectToAction("ContactDetail", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteContact(int id)
    {
        await _contactService.DeleteContactAsync(id);
        return RedirectToAction("Contacts");
    }

    // --- Companies ---
    public async Task<IActionResult> Companies()
    {
        var companies = await _companyService.GetAllAsync();
        return View(companies);
    }

    public async Task<IActionResult> CompanyDetail(int id)
    {
        var company = await _companyService.GetAsync(id);
        if (company == null) return NotFound();
        return View(company);
    }

    [HttpGet]
    public IActionResult CreateCompany() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCompany(string name, string? address)
    {
        await _companyService.CreateAsync(name, address);
        return RedirectToAction("Companies");
    }

    [HttpGet]
    public async Task<IActionResult> EditCompany(int id)
    {
        var company = await _companyService.GetAsync(id);
        if (company == null) return NotFound();
        return View(company);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCompany(int id, string name, string? address)
    {
        await _companyService.UpdateAsync(id, name, address);
        return RedirectToAction("CompanyDetail", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCompany(int id)
    {
        await _companyService.DeleteAsync(id);
        return RedirectToAction("Companies");
    }

    // --- Members ---
    public async Task<IActionResult> Members()
    {
        var users = await _userManager.Users
            .Include(u => u.Company)
            .ToListAsync();
        var userRoles = new Dictionary<string, IList<string>>();
        foreach (var user in users)
            userRoles[user.Id] = await _userManager.GetRolesAsync(user);
        ViewBag.UserRoles = userRoles;
        return View(users);
    }

    // Calendar placeholder (action for admin calendar view)
    public IActionResult Calendar() => View();
}
