namespace GoldenWhistle.Services.Interfaces
{
    public interface IChatService
    {
        Task<string> GetChatResponseAsync(string userMessage);
    }
}