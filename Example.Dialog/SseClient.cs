
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Example.Dialog;



/// <summary>
/// Event type
/// </summary>
public record DialogEventMessage(string EventType, string DialogRef, string ReportRef);


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

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            Console.WriteLine($"Event: {line}");
            var sseEvent = JsonSerializer.Deserialize<DialogEventMessage>(line, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            await eventHandler.Invoke(sseEvent!);
        }
    }
}
