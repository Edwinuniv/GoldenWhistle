using System.Text;
using System.Text.Json;
using GoldenWhistle.Services.Interfaces;

namespace GoldenWhistle.Services
{
    public class GeminiChatService : IChatService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiChatService> _logger;

        public GeminiChatService(IConfiguration configuration, ILogger<GeminiChatService> logger)
        {
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new Exception("Gemini API Key missing");
            _httpClient = new HttpClient();
            _logger = logger;
        }

        public async Task<string> GetChatResponseAsync(string userMessage)
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";

                var request = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = $"{GetSystemPrompt()}\n\nUser: {userMessage}" }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 500
                    }
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
                    return "Sorry, an error occurred with the assistant.";
                }

                using var doc = JsonDocument.Parse(responseJson);
                var reply = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return reply ?? "I didn't understand, can you rephrase?";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini chat error");
                return "Sorry, a technical error occurred. Please try again later.";
            }
        }

        private string GetSystemPrompt()
        {
            return @"You are 'WCH Assistant', an AI assistant for GoldenWhistle, a football fan platform for the 2026 World Cup.

You know these site sections:
- Bracket Challenge: predict match results and earn points
- Mood Map: vote for fan emotions (Ecstasy, Anxiety, Agony)
- Pub Finder: find bars to watch matches
- Data Visualizer: advanced stats (xG, possession, heat maps)
- Jersey Marketplace: buy/sell jerseys
- What If Simulator: simulate alternative results
- Kickoff Companion: match previews

Rules:
- Be friendly, enthusiastic, and concise
- Use emojis when appropriate
- If you don't know something, be honest
- Always suggest a concrete action (e.g., 'Go to the Bracket section to...')
- Don't give medical, financial, or legal advice
- Stay neutral on political opinions

Respond in the language user is speaking.";
        }
    }
}