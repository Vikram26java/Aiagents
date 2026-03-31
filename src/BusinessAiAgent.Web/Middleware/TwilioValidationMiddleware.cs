using BusinessAiAgent.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Twilio.Security;

namespace BusinessAiAgent.Web.Middleware;

public class TwilioValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _authToken;

    public TwilioValidationMiddleware(RequestDelegate next, IOptions<TwilioSettings> settings)
    {
        _next = next;
        _authToken = settings.Value.AuthToken;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate Twilio webhook paths
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/api/sms/webhook", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/voice/twiml", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/voice/gather", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/voice/status", StringComparison.OrdinalIgnoreCase))
        {
            // Skip validation in development
            if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                await _next(context);
                return;
            }

            var validator = new RequestValidator(_authToken);
            var signature = context.Request.Headers["X-Twilio-Signature"].FirstOrDefault() ?? "";
            var url = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";

            context.Request.EnableBuffering();
            var form = await context.Request.ReadFormAsync();
            var parameters = form.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

            if (!validator.Validate(url, parameters, signature))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Invalid Twilio signature");
                return;
            }

            context.Request.Body.Position = 0;
        }

        await _next(context);
    }
}
