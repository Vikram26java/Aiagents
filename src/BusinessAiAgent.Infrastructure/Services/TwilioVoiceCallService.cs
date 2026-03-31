using BusinessAiAgent.Core.Entities;
using BusinessAiAgent.Core.Enums;
using BusinessAiAgent.Core.Interfaces;
using BusinessAiAgent.Infrastructure.Data;
using BusinessAiAgent.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace BusinessAiAgent.Infrastructure.Services;

public class TwilioVoiceCallService : IVoiceCallService
{
    private readonly TwilioSettings _settings;
    private readonly AppDbContext _db;
    private readonly IChatService _chatService;

    public TwilioVoiceCallService(IOptions<TwilioSettings> settings, AppDbContext db, IChatService chatService)
    {
        _settings = settings.Value;
        _db = db;
        _chatService = chatService;
        TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
    }

    public async Task<string> MakeCallAsync(string to, string from, string webhookBaseUrl)
    {
        var conversation = new Conversation { Channel = ChannelType.Voice, Subject = $"Call to {to}" };
        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();

        var call = await CallResource.CreateAsync(
            to: new PhoneNumber(to),
            from: new PhoneNumber(from),
            url: new Uri($"{webhookBaseUrl}/api/voice/twiml?conversationId={conversation.Id}"),
            statusCallback: new Uri($"{webhookBaseUrl}/api/voice/status?conversationId={conversation.Id}"));

        _db.CallLogs.Add(new CallLog
        {
            ConversationId = conversation.Id,
            CallSid = call.Sid,
            From = from,
            To = to,
            Status = call.Status.ToString()
        });
        await _db.SaveChangesAsync();

        return call.Sid;
    }

    public string GenerateGreetingTwiml(string gatherUrl)
    {
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <Response>
                <Say voice="alice">Hello! I'm your business AI assistant. How can I help you today?</Say>
                <Gather input="speech" action="{gatherUrl}" speechTimeout="auto" language="en-US">
                    <Say voice="alice">Please speak after the tone.</Say>
                </Gather>
                <Say voice="alice">I didn't catch that. Goodbye!</Say>
            </Response>
            """;
    }

    public async Task<string> ProcessSpeechAsync(string speechResult, int conversationId)
    {
        // Store what user said
        _db.Messages.Add(new Message
        {
            ConversationId = conversationId,
            Direction = MessageDirection.Inbound,
            Body = speechResult
        });
        await _db.SaveChangesAsync();

        // Get conversation history
        var messages = await _db.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        // Get AI reply
        var aiReply = await _chatService.GetAiResponseAsync(messages, speechResult);

        // Store AI reply
        _db.Messages.Add(new Message
        {
            ConversationId = conversationId,
            Direction = MessageDirection.Outbound,
            Body = aiReply
        });
        await _db.SaveChangesAsync();

        return aiReply;
    }

    public string GenerateAiResponseTwiml(string aiText, string gatherUrl)
    {
        // Escape XML special chars
        var escaped = System.Security.SecurityElement.Escape(aiText);
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <Response>
                <Say voice="alice">{escaped}</Say>
                <Gather input="speech" action="{gatherUrl}" speechTimeout="auto" language="en-US">
                    <Say voice="alice">Is there anything else I can help with?</Say>
                </Gather>
                <Say voice="alice">Thank you for calling. Goodbye!</Say>
            </Response>
            """;
    }
}
