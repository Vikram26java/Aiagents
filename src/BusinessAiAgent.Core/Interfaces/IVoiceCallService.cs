namespace BusinessAiAgent.Core.Interfaces;

public interface IVoiceCallService
{
    Task<string> MakeCallAsync(string to, string from, string webhookBaseUrl);
    string GenerateGreetingTwiml(string gatherUrl);
    Task<string> ProcessSpeechAsync(string speechResult, int conversationId);
    string GenerateAiResponseTwiml(string aiText, string gatherUrl);
}
