using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using Sonata.Desktop.Models;

namespace Sonata.Desktop.Services;

public class ApiService(HttpClient? httpClient = null)
{
    private readonly HttpClient _httpClient = httpClient ?? new HttpClient
    {
        BaseAddress = new Uri("http://localhost:3000/v1/"),
    };
    
    public async Task<string> SendMessageAsync(string content, Guid conversationId)
    {
        var res = await _httpClient.PostAsJsonAsync("responses",
            new { content, conversationId });
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<ContinueConversationResponse>();
        return body?.Content ?? throw new InvalidOperationException("The API returned an empty response.");
    }

    public async Task<ObservableCollection<Conversation>> GetAllConversationsAsync()
    {
        var res = await _httpClient.GetAsync("conversations");
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<ConversationListResponse>();
        return new ObservableCollection<Conversation>(body?.Conversations ?? Array.Empty<Conversation>());
    }

    public async Task<ObservableCollection<Message>> GetMessagesAsync(Guid conversationId)
    {
        var res = await _httpClient.GetAsync($"conversations/{conversationId}/messages");
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<MessageResponse>();
        return new ObservableCollection<Message>(body?.Messages ?? Array.Empty<Message>());
    }
}
