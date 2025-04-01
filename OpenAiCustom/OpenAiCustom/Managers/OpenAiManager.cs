using OpenAI;
namespace OpenAiCustom.Managers;

public class OpenAiManager
{
    private static Dictionary<string, OpenAIClient> _clients = new();

    public static async Task<string> GetChat(string apiKey, string model, string text)
    {
        if (!_clients.ContainsKey(apiKey))
            _clients.Add(apiKey, new OpenAIClient(apiKey));

        var response = await _clients[apiKey].GetOpenAIResponseClient(model).CreateResponseAsync(text);
        return response.Value.GetOutputText();
    }
}
