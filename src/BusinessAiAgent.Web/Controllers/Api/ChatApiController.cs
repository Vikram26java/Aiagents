using BusinessAiAgent.Core.Enums;
using BusinessAiAgent.Core.Interfaces;
using BusinessAiAgent.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BusinessAiAgent.Web.Controllers.Api;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatApiController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IConversationService _conversationService;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatApiController(IChatService chatService, IConversationService conversationService, IHubContext<ChatHub> hubContext)
    {
        _chatService = chatService;
        _conversationService = conversationService;
        _hubContext = hubContext;
    }

    [HttpPost("conversations")]
    public async Task<IActionResult> StartConversation([FromBody] StartConversationRequest request)
    {
        var conversation = await _conversationService.CreateConversationAsync(
            ChannelType.Chat, request.Subject ?? "New Chat", request.ContactId);
        return Ok(new { conversation.Id, conversation.Channel, conversation.Subject, conversation.CreatedAt });
    }

    [HttpPost("conversations/{id}/messages")]
    public async Task<IActionResult> SendMessage(int id, [FromBody] SendMessageRequest request)
    {
        var conversation = await _conversationService.GetConversationAsync(id);
        if (conversation == null) return NotFound();

        // Store user message
        var userMsg = await _conversationService.AddMessageAsync(id, MessageDirection.Inbound, request.Message);

        // Get AI response
        var aiReply = await _chatService.GetAiResponseAsync(conversation.Messages, request.Message);
        var aiMsg = await _conversationService.AddMessageAsync(id, MessageDirection.Outbound, aiReply);

        // Push via SignalR
        await _hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveMessage", new
        {
            userMessage = new { userMsg.Id, userMsg.Body, userMsg.Direction, userMsg.CreatedAt },
            aiMessage = new { aiMsg.Id, aiMsg.Body, aiMsg.Direction, aiMsg.CreatedAt }
        });

        return Ok(new
        {
            userMessage = new { userMsg.Id, userMsg.Body, Direction = userMsg.Direction.ToString(), userMsg.CreatedAt },
            aiMessage = new { aiMsg.Id, aiMsg.Body, Direction = aiMsg.Direction.ToString(), aiMsg.CreatedAt }
        });
    }
}

public record StartConversationRequest(string? Subject, int? ContactId);
public record SendMessageRequest(string Message);
