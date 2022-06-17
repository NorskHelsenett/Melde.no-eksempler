using System;
using System.Net.Http;
using System.Threading.Tasks;
using OpenAPI;
using Microsoft.Extensions.DependencyInjection;
using MeldeApiReport;
using MeldeApiDialog;
using Example.Configuration;

namespace Example.Toveisdialog
{
    class Program
    {
        const string    UONSKET_HENDELSE_REF = "Uww35g";
        const int       MELDEORDNING_ID = 2;
        const bool      CREATE_DIALOG = true;


        static async Task Main(string[] args)
        {
            // Setup HTTP client with authentication for HelseId
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("MeldeNo", client =>
            {
                client.BaseAddress = Config.ApiUri;
            })
                .AddHttpMessageHandler(_ =>
                {
                    var scopes = new string[] { "nhn:melde/dialog/opprett", "nhn:melde/dialog/melding" };
                    return new JwkTokenHandler(Config.HelseIdUrl, Config.ClientId, Config.Jwk, scopes, Config.ClientType);
                });

            var provider = serviceCollection.BuildServiceProvider();
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            using var httpClient = httpClientFactory.CreateClient("MeldeNo");

            var dialogClient = new EksternDialogClient(httpClient);

            //
            // Check wether dialog exists
            //
            string dialogRef = null;
            try
            {
                var response = await dialogClient.GetDialogInfoAsync(UONSKET_HENDELSE_REF);
                dialogRef = response.DialogRef;
            }
            catch (ApiException e)
            {
                Console.WriteLine(e.StatusCode);
                Console.WriteLine(e.Message);
            }

            if(CREATE_DIALOG)
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

                    var createdResponse = await dialogClient.StartDialogAsync(createPayload);
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
