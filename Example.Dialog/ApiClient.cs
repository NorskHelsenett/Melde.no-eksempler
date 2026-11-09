using MeldeV2;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Example.Dialog;

internal class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GetDialogInfoResponse> GetDialog(string reportRef)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"api/v2/dialog/{reportRef}");
        var response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode) return null;

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GetDialogInfoResponse>(responseContent);
    }

    public async Task<DialogCreatedResponse> CreateDialog(CreateDialogInfo createInfo)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/v2/dialog");
        requestMessage.Content = JsonContent.Create(createInfo);
        var response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DialogCreatedResponse>(responseContent)!;
    }

    public async Task<DialogMessageCreatedResponse> SendMessage(CreateDialogMessageInfo messageInfo)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/v2/dialog/message");
        requestMessage.Content = JsonContent.Create(messageInfo);
        var response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DialogMessageCreatedResponse>(responseContent)!;
    }

    public async Task<GetUnreadDialogsResponse> GetUnreadDialogs()
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "api/v2/dialog/unreads");
        var response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GetUnreadDialogsResponse>(responseContent)!;
    }

    public async Task<bool> MarkAsRead(string dialogRef)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Patch, $"api/v2/dialog/{dialogRef}/markAsRead");
        var response = await _httpClient.SendAsync(requestMessage);
        return response.IsSuccessStatusCode;
    }

    public async Task<GetUnreadMessagesResponse> GetUnreadMessages(string dialogRef)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"api/v2/dialog/{dialogRef}/message?includeRead=false&includeAttachments=false");
        var response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GetUnreadMessagesResponse>(responseContent)!;
    }
}
