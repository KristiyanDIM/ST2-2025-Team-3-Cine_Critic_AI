using System.Text.Json;

namespace Cine_Critic_AI.Services.ChatStrategies
{
    public class AnalysisStrategy : IChatStrategy
    {
        public async Task<string> GenerateResponseAsync(string userMessage, int userId, LocalAIService ai)
        {
            var json = JsonSerializer.Serialize(new
            {
                model = "llama3",
                prompt = $"Отговаряй само на български език. Ти си AI филмов критик. Анализирай: {userMessage}"
            });
            return await ai.PostToOllamaAsync(json);
        }
    }
}
