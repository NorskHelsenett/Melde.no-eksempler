using Example.Configuration;
using MeldeV2;
using OpenAPI;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Example.Dialog
{
    class Program
    {
        const string UONSKET_HENDELSE_REF = "Vpk3qkx";
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
                    "<TULLE>" +
                    "Melding fra saksbehandler (API)",
                    SenderName = "Ola Nordmann",
                    Attachments = [
                        new AttachmentPart{
                            Name = "Bilde.jpg",
                            ContentType = "image/jpeg",
                            Content = "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"
                        }
                    ]
                };

                // Generated client
                //var messageResponse = await _dialogClient.MessagePOSTAsync(messagePayload);

                using var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/dialog/message");
                request.Content = JsonContent.Create(messagePayload);

#if true
                using var response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead);
                var jsonResponse = await response.Content.ReadAsStringAsync();
#endif
            }
            catch (ApiException e)
            {
                Console.WriteLine(e.StatusCode);
                Console.WriteLine(e.Message);
            }

            // Listen for events (SSE)
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
            if (dialogEvent.EventType == "clientRegistered")
            {
                Console.WriteLine($"Event received: ClientRegistered. Report area {dialogEvent.ReportArea}");
                if(dialogEvent.HasUnreadMessages ?? false)
                {
                    Console.WriteLine("Get all unread dialogs");
                    var unreadDialogs = await _dialogClient.UnreadsAsync();
                    foreach (var dialogRef in unreadDialogs.DialogRefs)
                    {
                        await ReadMessages(dialogRef);
                        await _dialogClient.MarkAsRead2Async(dialogRef);
                    }
                }
            }
            else if(dialogEvent.EventType == "newMessage")
            {
                if (!string.IsNullOrEmpty(dialogEvent.DialogRef))
                {
                    await ReadMessages(dialogEvent.DialogRef);
                    await _dialogClient.MarkAsRead2Async(dialogEvent.DialogRef);
                }
            }
            else if(dialogEvent.EventType == "readMessages")
            {
                // Receiver has read messages
            }
            else if(dialogEvent.EventType == "dialogDeleted")
            {
                // Dialog deleted
            }
        }

        private static async Task ReadMessages(string dialogRef)
        {
            var unreadMessages = await _dialogClient.MessageGETAsync(dialogRef, false, false);

            Console.WriteLine($"{unreadMessages.Messages.Count} new messages");

            foreach (var message in unreadMessages.Messages)
            {
                Console.WriteLine($"Content: {message.MessageText}");
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
}
