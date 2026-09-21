
using System;
using System.IO;
using System.Net.Http;
using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Example.Dialog;



/// <summary>
/// Event type
/// </summary>
public record DialogEventMessage(int ReportArea, string EventType, string DialogRef, string ReportRef, bool? HasUnreadMessages);


/// <summary>
/// SSE client
/// </summary>
class SseClient
{
    private readonly HttpClient _client;

    public SseClient(HttpClient client)
    {
        _client = client;
    }

    public async Task ListenAsync(
        string url,
        Func<DialogEventMessage, Task> eventHandler,
        CancellationToken cancellationToken = default)
    {
        // Make request, get the stream
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await _client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        await foreach (var item in SseParser.Create(stream).EnumerateAsync(cancellationToken))
        {
            Console.WriteLine($"Event type: {item.EventType}, Data: {item.Data}");

            var sseEvent = JsonSerializer.Deserialize<DialogEventMessage>(item.Data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            await eventHandler.Invoke(sseEvent!);
        }
    }
}
