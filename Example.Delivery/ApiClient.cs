using MeldeV2;
using System.Text.Json;

namespace Example.Delivery;

internal class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
