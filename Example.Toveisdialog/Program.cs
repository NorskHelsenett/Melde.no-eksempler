using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Example.Toveisdialog.OpenAPIs;
using MeldeApi;
using Microsoft.Extensions.DependencyInjection;

namespace Example.Toveisdialog
{
    class Program
    {
        // Points to Melde.no API base
        //private static readonly Uri ApiBaseAddress = new("https://localhost:44342/");
        private static readonly Uri ApiBaseAddress = new("https://api.test.melde.no/");

        // Points to the HelseId instance you want to use
        private static readonly string HelseIdUrl = "https://helseid-sts.test.nhn.no";

        static async Task Main(string[] args)
        {
            // Setup HTTP client with authentication for HelseId
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("MeldeNo", client =>
            {
                client.BaseAddress = ApiBaseAddress;
            })
                .AddHttpMessageHandler(_ =>
                {
                    // Provide your own client id and private key settings
                    var clientId = "<client id>";
                    var clientType = ClientType.Machine;
                    var scopes = new string[] { "nhn:melde/dialog/opprett", "nhn:melde/dialog/melding" };
                    var jwtPrivateKey = new Dictionary<string, object>
                    {
                        // ... key parts
                    };

                    return new JwkTokenHandler(HelseIdUrl, clientId, jwtPrivateKey, scopes, clientType);
                });

            var provider = serviceCollection.BuildServiceProvider();
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            using var httpClient = httpClientFactory.CreateClient("MeldeNo");

            var dialogClient = new EksternDialogClient(httpClient);

            const string refNr = "Uww35g";

            //
            // Check wether dialog exists
            //
            string dialogRef = null;
            try
            {
                var response = await dialogClient.GetDialogInfoAsync(refNr);
                dialogRef = response.DialogRef;
            }
            catch (ApiException e)
            {
                Console.WriteLine(e.StatusCode);
                Console.WriteLine(e.Message);
            }

            //
            // Create dialog if not existing. Will fail if dialog existed
            //
            try
            {
                var createPayload = new CreateDialogInfo
                {
                    ReportRef = refNr, // PromptForInput("RefNr"),
                    ReportArea = 2
                };

                var createdResponse = await dialogClient.StartDialogAsync(createPayload);
                dialogRef = createdResponse.DialogRef;
            }
            catch (ApiException e)
            {
                Console.WriteLine(e.StatusCode);
                Console.WriteLine(e.Message);
            }

            //
            // Write a message to the newly created dialog
            //
            try
            {
                var messagePayload = new CreateDialogMessageInfo
                {
                    DialogRef = dialogRef,
                    MessageText = "Hei hei! Melding fra API klient",
                    SenderName = "Ola Nordmann"
                };

                var messageResponse = await dialogClient.SendMessageAsync(messagePayload);
            }
            catch (ApiException e)
            {
                Console.WriteLine(e.StatusCode);
                Console.WriteLine(e.Message);
            }

            // Expected no unread messages at this point
            var messages = await dialogClient.GetUnreadMessagesAsync(dialogRef);

            // Manual work: Reply to message from web

            // Expected one or more unread messages at this point
            var unreadMessages = await dialogClient.GetUnreadMessagesAsync(dialogRef);
        }

        private static string PromptForInput(string message)
        {
            Console.Write($"{message}: ");
            return Console.ReadLine();
        }
    }
}
