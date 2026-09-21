using Example.Configuration;
using MeldeV2;
using OpenAPI;
using System.Text.Json;

namespace Example.Delivery;

class Program
{
    static Client? _deliveryClient;
    static HttpClient? _httpClient; 

    static async Task Main(string[] args)
    {
        int i = 0;
        int max = 1;
        try
        {
            _httpClient = CreateClient(["nhn:melde/delivery/full-access"]);
            _deliveryClient = new Client(_httpClient);

            // Call API, wait for response
            ReportInfoList listResponse = new();
            
            for(i=0; i < max; i++)
            {
                listResponse = await _deliveryClient.ListAsync(null, null);
            }

            if (listResponse.ReportInfos.Any())
            {
                var reportRef = listResponse.ReportInfos.FirstOrDefault()!.ReportRef;
                var getResponse = await _httpClient.GetAsync($"api/v2/Delivery/report/{reportRef}");
                var getResponseContent = await getResponse.Content.ReadAsStringAsync();
                var report = JsonSerializer.Deserialize<ReportDeliveryResponse>(getResponseContent);

                // Use generated client stub
                //var report = await _deliveryClient.ReportAsync(reportRef, false);

                foreach (var attachmentRef in report!.Header.AttachmentRefs)
                {
                    var file = await _deliveryClient.AttachmentAsync(reportRef, attachmentRef.AttachmentRef);


                    Console.WriteLine();
                }

                // Mark resport as read
                await _deliveryClient.MarkAsReadAsync(reportRef);
            }

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
                catch (Exception e)
                {
                    Console.WriteLine("Disconnected. Try again");
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
                Console.WriteLine($"Unfetched report {reportInfo.ReportRef}");
            }
        }

        if(deliveryEvent.EventType == "newReport" && !string.IsNullOrEmpty(deliveryEvent.ReportRef))
        {
            //Console.WriteLine("Wait");
            //await Task.Delay(5000);

            // Get report
            //var report = await _deliveryClient!.ReportAsync(deliveryEvent.ReportRef, false);
            var r = await _httpClient!.GetAsync($"api/v2/Delivery/report/{deliveryEvent.ReportRef}");
            var reportJson = await r.Content.ReadAsStringAsync();
            var report = JsonSerializer.Deserialize<ReportDeliveryResponse>(reportJson) ?? new();

            Console.WriteLine($"New report fetched: {report.Header.ReportRef}");

            await _deliveryClient.MarkAsReadAsync(deliveryEvent.ReportRef);
        }
    }

    private static HttpClient CreateClient(string[] scopes)
    {
        var htHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                // Accept even if the certificate is expired
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
