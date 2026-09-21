using System.Net.Mime;
using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;

namespace Example.Delivery;

/// <summary>
/// Event type
/// </summary>
public record DeliveryEventMessage(int reportArea, string EventType, string ReportRef, bool? HasNewReports);


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

        await foreach (var item in SseParser.Create(stream).EnumerateAsync(cancellationToken))
        {
            Console.WriteLine($"Event type: {item.EventType}, Data: {item.Data}");

            var sseEvent = JsonSerializer.Deserialize<DeliveryEventMessage>(item.Data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            await eventHandler.Invoke(sseEvent!);
        }
    }
}
