using BusinessAiAgent.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace BusinessAiAgent.Web.Hubs;

public class ChatHub : Hub
{
    private readonly UserManager<AppUser> _userManager;

    public ChatHub(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }

    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
    }

    public async Task JoinDirectConversation(int conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"dm_{conversationId}");
    }

    public async Task LeaveDirectConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"dm_{conversationId}");
    }

    public async Task JoinUserChannel(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var appUser = await _userManager.GetUserAsync(user);
            if (appUser != null)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{appUser.Id}");
        }
        await base.OnConnectedAsync();
    }
}
