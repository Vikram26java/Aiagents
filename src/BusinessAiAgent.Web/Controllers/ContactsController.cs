using BusinessAiAgent.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessAiAgent.Web.Controllers;

[Authorize]
public class ContactsController : Controller
{
    private readonly IContactService _contactService;

    public ContactsController(IContactService contactService)
    {
        _contactService = contactService;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var contacts = await _contactService.GetContactsAsync(search);
        ViewBag.Search = search;
        return View(contacts);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var contact = await _contactService.GetContactAsync(id);
        if (contact == null) return NotFound();
        return View(contact);
    }
}
