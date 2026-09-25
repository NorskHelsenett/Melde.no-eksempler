using Example.Configuration;
using MeldeV2;
using OpenAPI;
using System.Text.Json;

namespace Example.Delivery;

class Program
{
    static Client? _deliveryClient;
    static HttpClient _httpClient = new();

    static async Task Main(string[] args)
    {
        try
        {
            _httpClient = CreateClient(["nhn:melde/delivery/full-access"]);
            _deliveryClient = new Client(_httpClient);

            // Listen for events (SSE)
            var sseClient = new SseClient(_httpClient);
            while (true)
            {
                try
                {
                    await sseClient.ListenAsync("api/v2/delivery/events", HandleEvents);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Cancelled");
                    break;
                }
                catch (HttpRequestException)
                {
                    Console.WriteLine("Delivery events disconnected. Try to connect.");
                    await Task.Delay(5000);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Communication error. Try to connect. Exception: {e.Message}");
                    await Task.Delay(5000);
                }
            }
        }
        catch (ApiException ex)
        {
            Console.WriteLine("-- Feil");
            Console.WriteLine($"HTTP statuskode: {ex.StatusCode}");
            Console.WriteLine($"Feilemdling: {ex.Message}");
        }
    }

    private static async Task HandleEvents(DeliveryEventMessage deliveryEvent)
    {
        Console.WriteLine($"New event: {deliveryEvent.EventType}");

        if (deliveryEvent.EventType == "clientRegistered")
        {
            Console.WriteLine("Get all unread dialogs");
            var unfetchedReports = await _deliveryClient!.ListAsync(null, null);
            foreach (var reportInfo in unfetchedReports.ReportInfos)
            {
                // Get report
                await DownloadReportWithAttachments(reportInfo.ReportRef);
            }
        }
        else if(deliveryEvent.EventType == "newReport" && !string.IsNullOrEmpty(deliveryEvent.ReportRef))
        {
            // Get report
            await DownloadReportWithAttachments(deliveryEvent.ReportRef);
        }
    }

    private static async Task DownloadReportWithAttachments(string reportRef)
    {
        ApiClient apiClient = new(_httpClient);

        var report = await apiClient.DownloadReport(reportRef);
        if (report is null)
        {
            Console.WriteLine("No report was downloaded");
            return;
        }

        Console.WriteLine($"Report {report.Header.ReportRef} fetched with {report.Header.AttachmentRefs.Count} attachments:");

        foreach (var attachmentRef in report!.Header.AttachmentRefs)
        {
            var att = await apiClient.DownloadAttachment(reportRef, attachmentRef.AttachmentRef);
            Console.WriteLine($"  Attachment: Name={att.FileName}, ContentType={att.ContentType}, Size={att.Content.Length}");
        }

        // Mark report as read
        if(!await apiClient.MarkReportAsRead(reportRef))
        {
            Console.WriteLine($"Failed to mark report {reportRef} as read");
        }
    }


    private static HttpClient CreateClient(string[] scopes)
    {
        var htHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                // Accept even if the certificate is expired. For test environments only.
                if (errors == System.Net.Security.SslPolicyErrors.None ||
                    (errors & System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors) == System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors ||
                    (errors & System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch) == System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch)
                {
                    return true;
                }

                return false;
            },

        };

        var jwtHandler = new JwkTokenHandler(Config.HelseIdUrl, Config.ClientId, Config.Jwk, scopes, htHandler);

        var httpClient = new HttpClient(jwtHandler)
        {
            BaseAddress = Config.ApiUri,
        };

        return httpClient;
    }
}
