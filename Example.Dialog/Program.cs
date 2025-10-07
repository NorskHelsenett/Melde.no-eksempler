using Example.Configuration;
using MeldeV2;
using OpenAPI;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Example.Toveisdialog
{
    class Program
    {
        const string UONSKET_HENDELSE_REF = "Vm59nbj";
        const int MELDEORDNING_ID = 10; //2: bivir,  10:biovig, 8:alvor, 11:radiation, 7: med.equipment
        const bool CREATE_DIALOG = true;

        static Client _dialogClient = null;

        static async Task Main(string[] args)
        {
            var httpClient = CreateClient(["nhn:melde/dialog/opprett", "nhn:melde/dialog/melding"]);
            _dialogClient = new Client(httpClient);

            //
            // Check dialog existence
            //
            string dialogRef = null;
            try
            {
                var response = await _dialogClient.DialogGETAsync(UONSKET_HENDELSE_REF);
                dialogRef = response.DialogRef;
            }
            catch (ApiException e)
            {
                Console.WriteLine(e.StatusCode);
                Console.WriteLine(e.Message);
            }

            if (CREATE_DIALOG && string.IsNullOrWhiteSpace(dialogRef))
            {
                //
                // Create dialog if not existing. Will fail if dialog existed
                //
                try
                {
                    var createPayload = new CreateDialogInfo
                    {
                        ReportRef = UONSKET_HENDELSE_REF,
                        ReportArea = MELDEORDNING_ID
                    };

                    var createdResponse = await _dialogClient.DialogPOSTAsync(createPayload);
                    dialogRef = createdResponse.DialogRef;
                }
                catch (ApiException e)
                {
                    Console.WriteLine(e.StatusCode);
                    Console.WriteLine(e.Message);
                }
            }

            //
            // Write a message to the newly created dialog
            //
            try
            {
                var messagePayload = new CreateDialogMessageInfo
                {
                    DialogRef = dialogRef,
                    MessageText = "Hei \nhei! <p/> Med vedlegg." +
                    "<br/>" +
                    "<ul>" +
                    "<li>Punkt 1</li>" +
                    "<li>Punkt 2</li>" +
                    "<li>Punkt 3</li>" +
                    "</ul>" +
                    "Liste ferdig<br/>" +
                    "Ny linje<p>" +
                    "Melding fra saksbehandler (API)",
                    SenderName = "Ola Nordmann",
                };

                var messageResponse = await _dialogClient.MessagePOSTAsync(messagePayload);
            }
            catch (ApiException e)
            {
                Console.WriteLine(e.StatusCode);
                Console.WriteLine(e.Message);
            }

            // Expected no unread messages at this point
            var messages = await _dialogClient.MessageGETAsync(dialogRef);

            // Listen for events (SSE)
            // I.e. reply messages
            var sseClient = new SseClient(httpClient);
            while (true)
            {
                try
                {
                    await sseClient.ListenAsync("api/v2/dialog/events", HandleEvents);
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

        private static async Task HandleEvents(DialogEventMessage dialogEvent)
        {
            Console.WriteLine($"New event: {dialogEvent.EventType}");

            if (!string.IsNullOrEmpty(dialogEvent.DialogRef))
            {
                // Get unread messages
                var unreadMessages = await _dialogClient.MessageGETAsync(dialogEvent.DialogRef);

                Console.WriteLine($"{unreadMessages.Messages.Count} new messages");

                foreach (var message in unreadMessages.Messages) {
                    Console.WriteLine($"Content: {message.MessageText}");
                }
            }
        }

        private static string PromptForInput(string message)
        {
            Console.Write($"{message}: ");
            return Console.ReadLine();
        }

        private static HttpClient CreateClient(string[] scopes)
        {
            var htHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    // Accept even if the certificate is expired
                    if (errors == System.Net.Security.SslPolicyErrors.None ||
                        errors == System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors ||
                        errors == System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch)
                    {
                        return true;
                    }

                    return false;
                },

            };

            var jwtHandler = new JwkTokenHandler(Config.HelseIdUrl, Config.ClientId, Config.Jwk, scopes, Config.ClientType, Config.TokenType, htHandler);

            var httpClient = new HttpClient(jwtHandler)
            {
                BaseAddress = Config.ApiUri,
            };

            return httpClient;
        }
    }

    public record DialogEventMessage(string EventType, string DialogRef, string ReportRef);


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
}
