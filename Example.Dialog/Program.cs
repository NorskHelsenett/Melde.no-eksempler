using System;
using System.Net.Http;
using System.Threading.Tasks;
using OpenAPI;
using Microsoft.Extensions.DependencyInjection;
using Example.Configuration;
using System.Collections.Generic;
using MeldeV2;
namespace Example.Toveisdialog
{
    class Program
    {
        const string    UONSKET_HENDELSE_REF = "Vx5pmxq";
        const int       MELDEORDNING_ID = 10; //2: bivir,  10:biovig, 8:alvor
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
                    return new JwkTokenHandler(Config.HelseIdUrl, Config.ClientId, Config.Jwk, scopes, Config.ClientType, Config.TokenType);
                });

            var provider = serviceCollection.BuildServiceProvider();
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            using var httpClient = httpClientFactory.CreateClient("MeldeNo");

            var dialogClient = new Client(httpClient);

            //
            // Check wether dialog exists
            //
            string dialogRef = null;
            try
            {
                var response = await dialogClient.DialogGETAsync(UONSKET_HENDELSE_REF);
                dialogRef = response.DialogRef;
            }
            catch (ApiException e)
            {
                Console.WriteLine(e.StatusCode);
                Console.WriteLine(e.Message);
            }

            if(CREATE_DIALOG && string.IsNullOrWhiteSpace(dialogRef))
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

                    var createdResponse = await dialogClient.DialogPOSTAsync(createPayload);
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
                    "Bilde:<br/>" +
                    "<img src=\"data:image/png;base64," +
                    "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" + "\"/>" +
                    "Ny linje<p>" +
                    "Melding fra saksbehandler (API)",
                    SenderName = "Ola Nordmann",
                    Attachments = new List<Attachment>
                    {
                        new Attachment
                        {
                            Name ="fil.txt",
                            Content = "SW5uaG9sZCBpIGZpbAo="
                        },
                        new Attachment
                        {
                            Name = "fil2.txt",
                            Content = "RGVubmUgZmlsYSBoYXIgZ2Fuc2tlIG15ZSBtZXIgaW5uaG9sZCBlbm4gZGVuIGbDuHJzdGUK"
                        }
                    }
                };

                var messageResponse = await dialogClient.MessagePOSTAsync(messagePayload);
            }
            catch (ApiException e)
            {
                Console.WriteLine(e.StatusCode);
                Console.WriteLine(e.Message);
            }

            // Expected no unread messages at this point
            var messages = await dialogClient.MessageGETAsync(dialogRef);

            // Manual work: Reply to message from web

            // Expected one or more unread messages at this point
            var unreadMessages = await dialogClient.MessageGETAsync(dialogRef);
        }

        private static string PromptForInput(string message)
        {
            Console.Write($"{message}: ");
            return Console.ReadLine();
        }
    }
}
