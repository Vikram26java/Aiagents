using BusinessAiAgent.Core.Entities;
using BusinessAiAgent.Core.Enums;
using BusinessAiAgent.Core.Interfaces;
using BusinessAiAgent.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusinessAiAgent.Web.Controllers.Api;

[ApiController]
[Route("api/calendar")]
[Authorize]
public class CalendarApiController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _db;

    public CalendarApiController(IConversationService conversationService, UserManager<AppUser> userManager, AppDbContext db)
    {
        _conversationService = conversationService;
        _userManager = userManager;
        _db = db;
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents([FromQuery] DateTime? start, [FromQuery] DateTime? end, [FromQuery] string? channel)
    {
        var user = await _userManager.GetUserAsync(User);
        var isAdmin = user != null && await _userManager.IsInRoleAsync(user, "Admin");

        var events = new List<object>();

        // Only fetch conversation events if no channel filter or a valid channel
        if (channel != "Meeting")
        {
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

            events.AddRange(conversations.Select(c => new
            {
                id = $"conv-{c.Id}",
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
                extendedProps = new { type = "conversation", channel = c.Channel.ToString(), messages = c.Messages.Count }
            }));
        }

        // Fetch meetings
        if (string.IsNullOrEmpty(channel) || channel == "Meeting")
        {
            var meetingsQuery = _db.Meetings.AsQueryable();

            if (!isAdmin)
                meetingsQuery = meetingsQuery.Where(m => m.CreatedByUserId == user!.Id);

            if (start.HasValue)
                meetingsQuery = meetingsQuery.Where(m => m.EndTime >= start.Value);
            if (end.HasValue)
                meetingsQuery = meetingsQuery.Where(m => m.StartTime <= end.Value);

            var meetings = await meetingsQuery.Include(m => m.CreatedByUser).ToListAsync();

            events.AddRange(meetings.Select(m => new
            {
                id = $"mtg-{m.Id}",
                title = m.Title,
                start = m.StartTime.ToString("o"),
                end = m.EndTime.ToString("o"),
                color = "#8b5cf6",
                url = "",
                extendedProps = new { type = "meeting", meetingId = m.Id, description = m.Description ?? "", createdBy = m.CreatedByUser?.FullName ?? "" }
            }));
        }

        return Ok(events);
    }

    [HttpPost("meetings")]
    public async Task<IActionResult> CreateMeeting([FromBody] CreateMeetingRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { error = "Title is required." });

        if (request.StartTime >= request.EndTime)
            return BadRequest(new { error = "End time must be after start time." });

        var meeting = new Meeting
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            CreatedByUserId = user.Id
        };

        _db.Meetings.Add(meeting);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            id = meeting.Id,
            title = meeting.Title,
            start = meeting.StartTime.ToString("o"),
            end = meeting.EndTime.ToString("o")
        });
    }

    [HttpDelete("meetings/{id}")]
    public async Task<IActionResult> DeleteMeeting(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var meeting = await _db.Meetings.FindAsync(id);
        if (meeting == null) return NotFound();

        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (!isAdmin && meeting.CreatedByUserId != user.Id)
            return Forbid();

        _db.Meetings.Remove(meeting);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    public class CreateMeetingRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
