using BusinessAiAgent.Core.Entities;
using BusinessAiAgent.Core.Enums;
using BusinessAiAgent.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BusinessAiAgent.Web.Controllers.Api;

[ApiController]
[Route("api/calendar")]
[Authorize]
public class CalendarApiController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly UserManager<AppUser> _userManager;

    public CalendarApiController(IConversationService conversationService, UserManager<AppUser> userManager)
    {
        _conversationService = conversationService;
        _userManager = userManager;
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents([FromQuery] DateTime? start, [FromQuery] DateTime? end, [FromQuery] string? channel)
    {
        var user = await _userManager.GetUserAsync(User);
        var isAdmin = user != null && await _userManager.IsInRoleAsync(user, "Admin");

        ChannelType? channelFilter = null;
        if (Enum.TryParse<ChannelType>(channel, out var ch))
            channelFilter = ch;

        List<Conversation> conversations;
        if (isAdmin)
        {
            conversations = await _conversationService.GetConversationsAsync(channelFilter);
        }
        else
        {
            conversations = user?.ContactId != null
                ? await _conversationService.GetConversationsForContactAsync(user.ContactId.Value)
                : [];
            if (channelFilter.HasValue)
                conversations = conversations.Where(c => c.Channel == channelFilter.Value).ToList();
        }

        if (start.HasValue)
            conversations = conversations.Where(c => c.CreatedAt >= start.Value).ToList();
        if (end.HasValue)
            conversations = conversations.Where(c => c.CreatedAt <= end.Value).ToList();

        var events = conversations.Select(c => new
        {
            id = c.Id,
            title = c.Subject,
            start = c.CreatedAt.ToString("o"),
            end = c.ClosedAt?.ToString("o") ?? c.CreatedAt.AddHours(1).ToString("o"),
            color = c.Channel switch
            {
                ChannelType.Chat => "#0d6efd",
                ChannelType.Voice => "#198754",
                ChannelType.Sms => "#ffc107",
                ChannelType.Email => "#dc3545",
                ChannelType.Direct => "#212529",
                _ => "#6c757d"
            },
            url = isAdmin ? $"/Admin/ConversationDetail/{c.Id}" : $"/Customer/ConversationDetail/{c.Id}",
            extendedProps = new { channel = c.Channel.ToString(), messages = c.Messages.Count }
        });

        return Ok(events);
    }
}
