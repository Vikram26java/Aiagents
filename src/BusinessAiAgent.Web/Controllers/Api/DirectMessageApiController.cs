using BusinessAiAgent.Core.Entities;
using BusinessAiAgent.Core.Interfaces;
using BusinessAiAgent.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BusinessAiAgent.Web.Controllers.Api;

[ApiController]
[Route("api/dm")]
[Authorize]
public class DirectMessageApiController : ControllerBase
{
    private readonly IDirectMessageService _dmService;
    private readonly UserManager<AppUser> _userManager;
    private readonly IHubContext<ChatHub> _hubContext;

    public DirectMessageApiController(
        IDirectMessageService dmService,
        UserManager<AppUser> userManager,
        IHubContext<ChatHub> hubContext)
    {
        _dmService = dmService;
        _userManager = userManager;
        _hubContext = hubContext;
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var conversations = await _dmService.GetDirectConversationsAsync(user.Id);

        var result = new List<object>();
        foreach (var c in conversations)
        {
            // Extract the other user's ID from subject "DM:{id1}:{id2}"
            var parts = c.Subject.Split(':');
            var otherUserId = parts.Length == 3
                ? (parts[1] == user.Id ? parts[2] : parts[1])
                : null;
            var otherUser = otherUserId != null ? await _userManager.FindByIdAsync(otherUserId) : null;

            result.Add(new
            {
                c.Id,
                otherUserId,
                otherUserName = otherUser?.FullName ?? "Unknown",
                lastMessage = c.Messages.LastOrDefault()?.Body,
                lastMessageAt = c.Messages.LastOrDefault()?.CreatedAt,
                messageCount = c.Messages.Count
            });
        }

        return Ok(result);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartConversation([FromBody] StartDmRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var conversation = await _dmService.GetOrCreateDirectConversationAsync(user.Id, request.OtherUserId);
        return Ok(new { conversation.Id });
    }

    [HttpPost("conversations/{id}/messages")]
    public async Task<IActionResult> SendMessage(int id, [FromBody] DmMessageRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var message = await _dmService.SendDirectMessageAsync(id, user.Id, request.Message);

        // Push via SignalR to the DM conversation group
        await _hubContext.Clients.Group($"dm_{id}").SendAsync("ReceiveDirectMessage", new
        {
            message.Id,
            message.Body,
            message.SenderId,
            senderName = user.FullName,
            message.CreatedAt
        });

        return Ok(new
        {
            message.Id,
            message.Body,
            message.SenderId,
            senderName = user.FullName,
            message.CreatedAt
        });
    }

    [HttpGet("members")]
    public async Task<IActionResult> GetMembers()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        // Get members from the same company
        var query = _userManager.Users.Where(u => u.Id != user.Id);
        if (user.CompanyId.HasValue)
            query = query.Where(u => u.CompanyId == user.CompanyId);

        var members = await query.Select(u => new
        {
            u.Id,
            u.FullName,
            u.Email
        }).ToListAsync();

        return Ok(members);
    }

    [HttpGet("conversations/{id}/messages")]
    public async Task<IActionResult> GetMessages(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var conversations = await _dmService.GetDirectConversationsAsync(user.Id);
        var conv = conversations.FirstOrDefault(c => c.Id == id);
        if (conv == null) return NotFound();

        var messages = conv.Messages.Select(m => new
        {
            m.Id,
            m.Body,
            m.SenderId,
            m.CreatedAt
        });

        return Ok(messages);
    }
}

public record StartDmRequest(string OtherUserId);
public record DmMessageRequest(string Message);
