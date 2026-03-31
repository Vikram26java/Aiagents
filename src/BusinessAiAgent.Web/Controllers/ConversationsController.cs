using BusinessAiAgent.Core.Enums;
using BusinessAiAgent.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessAiAgent.Web.Controllers;

[Authorize]
public class ConversationsController : Controller
{
    private readonly IConversationService _conversationService;

    public ConversationsController(IConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    public async Task<IActionResult> Index(string? channel)
    {
        ChannelType? filter = null;
        if (Enum.TryParse<ChannelType>(channel, out var ch))
            filter = ch;

        var conversations = await _conversationService.GetConversationsAsync(filter);
        ViewBag.ChannelFilter = channel;
        return View(conversations);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var conversation = await _conversationService.GetConversationAsync(id);
        if (conversation == null) return NotFound();
        return View(conversation);
    }
}
