namespace BusinessAiAgent.Infrastructure.Settings;

public class OpenAiSettings
{
    public const string SectionName = "OpenAi";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public string SystemPrompt { get; set; } = "You are a helpful business assistant. Be professional, concise, and helpful.";
}
