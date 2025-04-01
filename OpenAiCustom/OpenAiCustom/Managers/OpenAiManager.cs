using OpenAI;
namespace OpenAiCustom.Managers;

public class OpenAiManager
{
    private static OpenAIClient _client;

    public static void Initialize()
    {
        _client = new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
    }

    public static async Task<string> GetChat(string text)
    {
        var response = await _client.GetOpenAIResponseClient("o1").CreateResponseAsync(text);
        return response.Value.GetOutputText();
    }
}
