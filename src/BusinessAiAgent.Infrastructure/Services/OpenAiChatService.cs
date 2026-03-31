using System.Text;
using System.Text.Json;
using BusinessAiAgent.Core.Entities;
using BusinessAiAgent.Core.Enums;
using BusinessAiAgent.Core.Interfaces;
using BusinessAiAgent.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace BusinessAiAgent.Infrastructure.Services;

public class OpenAiChatService : IChatService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiSettings _settings;

    public OpenAiChatService(IHttpClientFactory httpClientFactory, IOptions<OpenAiSettings> settings)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
    }

    public async Task<string> GetAiResponseAsync(IEnumerable<Message> conversationHistory, string userMessage)
    {
        var messages = new List<object>
        {
            new { role = "system", content = _settings.SystemPrompt }
        };

        foreach (var msg in conversationHistory)
        {
            messages.Add(new
            {
                role = msg.Direction == MessageDirection.Inbound ? "user" : "assistant",
                content = msg.Body
            });
        }

        messages.Add(new { role = "user", content = userMessage });

        var requestBody = new
        {
            model = _settings.Model,
            messages,
            max_tokens = 1000,
            temperature = 0.7
        };

        var client = _httpClientFactory.CreateClient("OpenAI");
        var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions");
        request.Headers.Add("Authorization", $"Bearer {_settings.ApiKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "I'm sorry, I couldn't generate a response.";
    }
}
