using MeldeV2;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Example.AdverseIncident;

internal class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<KvitteringsMelding> ReportAdverseIncident(AdverseIncidentRequest requestData, AttachmentPart[] attachments)
    {
        using var content = new MultipartFormDataContent();

        content.Add(
            content: JsonContent.Create(requestData, options: new JsonSerializerOptions { PropertyNamingPolicy = null }),
            name: "adverseIncidentRequest"
        );

        foreach (var attachment in attachments)
        {
            var fileStream = new MemoryStream(Convert.FromBase64String(attachment.Content));
            content.Add(
                content: new StreamContent(fileStream).WithContentType(attachment.ContentType),
                name: "attachments",
                fileName: attachment.Name
            );
        }

        var response = await _httpClient.PostAsync("api/v2/report/adverse-incident", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<KvitteringsMelding>(responseContent)!;
    }
}

internal static class StreamContentExtension
{
    public static StreamContent WithContentType(this StreamContent content, string contentType)
    {
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return content;
    }
}
