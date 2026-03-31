using BusinessAiAgent.Core.Interfaces;
using BusinessAiAgent.Infrastructure.Data;
using BusinessAiAgent.Infrastructure.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BusinessAiAgent.Web.Controllers.Api;

[ApiController]
[Route("api/voice")]
public class VoiceApiController : ControllerBase
{
    private readonly IVoiceCallService _voiceService;
    private readonly TwilioSettings _twilioSettings;
    private readonly AppDbContext _db;

    public VoiceApiController(IVoiceCallService voiceService, IOptions<TwilioSettings> twilioSettings, AppDbContext db)
    {
        _voiceService = voiceService;
        _twilioSettings = twilioSettings.Value;
        _db = db;
    }

    [HttpPost("make")]
    public async Task<IActionResult> MakeCall([FromBody] MakeCallRequest request)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var callSid = await _voiceService.MakeCallAsync(
            request.To, request.From ?? _twilioSettings.PhoneNumber, baseUrl);
        return Ok(new { callSid });
    }

    [HttpPost("twiml")]
    [Consumes("application/x-www-form-urlencoded")]
    public IActionResult Twiml([FromQuery] int conversationId)
    {
        var gatherUrl = $"{Request.Scheme}://{Request.Host}/api/voice/gather?conversationId={conversationId}";
        var twiml = _voiceService.GenerateGreetingTwiml(gatherUrl);
        return Content(twiml, "application/xml");
    }

    [HttpPost("gather")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Gather([FromQuery] int conversationId, [FromForm] IFormCollection form)
    {
        var speechResult = form["SpeechResult"].ToString();
        if (string.IsNullOrEmpty(speechResult))
        {
            return Content("""
                <?xml version="1.0" encoding="UTF-8"?>
                <Response><Say voice="alice">I didn't catch that. Goodbye!</Say></Response>
                """, "application/xml");
        }

        var aiReply = await _voiceService.ProcessSpeechAsync(speechResult, conversationId);
        var gatherUrl = $"{Request.Scheme}://{Request.Host}/api/voice/gather?conversationId={conversationId}";
        var twiml = _voiceService.GenerateAiResponseTwiml(aiReply, gatherUrl);
        return Content(twiml, "application/xml");
    }

    [HttpPost("status")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Status([FromQuery] int conversationId, [FromForm] IFormCollection form)
    {
        var callSid = form["CallSid"].ToString();
        var status = form["CallStatus"].ToString();
        var duration = form["CallDuration"].ToString();

        var callLog = await _db.CallLogs.FirstOrDefaultAsync(c => c.CallSid == callSid);
        if (callLog != null)
        {
            callLog.Status = status;
            if (int.TryParse(duration, out var dur))
                callLog.DurationSeconds = dur;
            await _db.SaveChangesAsync();
        }

        return Ok();
    }
}

public record MakeCallRequest(string To, string? From);
