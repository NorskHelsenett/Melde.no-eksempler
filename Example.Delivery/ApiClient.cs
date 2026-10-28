using MeldeV2;
using System.Net.Mime;
using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;

namespace Example.Delivery;

/// <summary>
/// Event type
/// </summary>
internal record DeliveryEventMessage(int reportArea, string EventType, string ReportRef, bool? HasNewReports);

internal class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task ListenForEvents(
        Func<DeliveryEventMessage, Task> eventHandler,
        CancellationToken cancellationToken = default)
    {
        // Make request, get the stream
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/delivery/events");
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(MediaTypeNames.Text.EventStream));

        using var response = await _httpClient.SendAsync(
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

    public async Task<ReportInfoList> ListReports(int? reportArea, string? fromDate)
    {
        List<string> queryParamsList = new();
        if (reportArea is not null) queryParamsList.Add($"reportArea={reportArea}");
        if (fromDate is not null) queryParamsList.Add($"fromDate={fromDate}");

        var queryParams = queryParamsList.Any() ? "?" + string.Join("&", queryParamsList) : "";
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "api/v2/delivery/list" + queryParams);
        var response = await _httpClient.SendAsync(requestMessage);
        var getResponseContent = await response.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<ReportInfoList>(getResponseContent);
        return list!;
    }

    public async Task<ReportDeliveryResponse?> DownloadReport(string reportRef)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"api/v2/delivery/report/{reportRef}");
        var response = await _httpClient.SendAsync(requestMessage);
        var getResponseContent = await response.Content.ReadAsStringAsync();
        var report = JsonSerializer.Deserialize<ReportDeliveryResponse>(getResponseContent);

        if (report is null) return null;

        List<Attachment> attachments = new();
        foreach (var attachmentRef in report!.Header.AttachmentRefs)
        {
            var attachment = await DownloadAttachment(reportRef, attachmentRef.AttachmentRef);
            attachments.Add(attachment);
            Console.WriteLine();
        }

        return report;
    }

    public async Task<Attachment> DownloadAttachment(string reportRef, string attachmentRef)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"api/v2/delivery/report/{reportRef}/attachment/{attachmentRef}");
        var response = await _httpClient.SendAsync(requestMessage);

        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
         ?? response.Content.Headers.ContentDisposition?.FileName
         ?? $"file-{Guid.NewGuid()}";

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

        return new Attachment(fileName, contentType, fileBytes);
    }

    public async Task<bool> MarkReportAsRead(string reportRef)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Patch, $"api/v2/delivery/report/{reportRef}/markAsRead");
        var response = await _httpClient.SendAsync(requestMessage);
        return response.IsSuccessStatusCode;
    }
}
