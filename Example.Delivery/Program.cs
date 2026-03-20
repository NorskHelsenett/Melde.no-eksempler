using Example.Configuration;
using MeldeV2;
using OpenAPI;

namespace Example.Delivery;

class Program
{
    static Client? _deliveryClient;

    static async Task Main(string[] args)
    {
        try
        {
            var httpClient = CreateClient(["nhn:melde/delivery/full-access"]);
            _deliveryClient = new Client(httpClient);

            // Call API, wait for response
            var response = await _deliveryClient.ListAsync(null);

            var reportRef = response.ReportRefs.FirstOrDefault()!.ReportRef;

            //var r = await httpClient.GetAsync($"api/v2/Delivery/report/{reportRef}");
            //var rc = await r.Content.ReadAsStringAsync();
            //var js = JsonSerializer.Deserialize<ReportDeliveryResponse>(rc);

            var response2 = await _deliveryClient.ReportAsync(reportRef);

            foreach(var attachmentRef in response2.Header.AttachmentRefs)
            {
                var file = await _deliveryClient.AttachmentAsync(reportRef, attachmentRef.AttachmentRef);

                Console.WriteLine();
            }

            // Listen for events (SSE)
            var sseClient = new SseClient(httpClient);
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
                catch (Exception)
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
            var unfetchedReports = await _deliveryClient!.ListAsync(null);
            foreach (var reportRef in unfetchedReports.ReportRefs)
            {
                Console.WriteLine($"Unfetched report {reportRef}");
            }
        }

        if(deliveryEvent.EventType == "newReport" && !string.IsNullOrEmpty(deliveryEvent.ReportRef))
        {
            // Get report
            var report = await _deliveryClient!.ReportAsync(deliveryEvent.ReportRef);

            Console.WriteLine($"New report fetched: {report.Header.ReportRef}");
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
