using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Example.Delivery;

/// <summary>
/// Event type
/// </summary>
public record DeliveryEventMessage(string EventType, string ReportRef);


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
        Func<DeliveryEventMessage, Task> eventHandler,
        CancellationToken cancellationToken = default)
    {
        // Make request, get the stream
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(MediaTypeNames.Text.EventStream));

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
            var sseEvent = JsonSerializer.Deserialize<DeliveryEventMessage>(line, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            await eventHandler.Invoke(sseEvent!);
        }
    }
}
