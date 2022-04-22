using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Example.Configuration;
using MeldeApi;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenAPI;

namespace Example.Kosmetikk
{
    class Program
    {
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
                // Auth params can be set in AuthParams.cs
                return new JwkTokenHandler(Config.HelseIdUrl, Config.ClientId, Config.Jwk, new[] { "nhn:melde/kosttilskudd" }, Config.ClientType);
            });

            var provider = serviceCollection.BuildServiceProvider();
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            using var httpClient = httpClientFactory.CreateClient("MeldeNo");

            //// Fill out request data
            var requestData = new KosttilskuddRequest()
            {
                Hode = new HodePartOfKosttilskuddMelderPartAndKosttilskuddHendelsePartAndKosttilskuddPasientPart
                {
                    EksternSaksId = Guid.NewGuid().ToString(),
                    Melder = new KosttilskuddMelderPart
                    {
                        Fødselsnummer = "13075706604",
                        Epost = "TestData@melde.no",
                        Organisasjonsnummer = "883974832"
                    },
                    Hendelse = new KosttilskuddHendelsePart
                    {
                        HvaSkjedde = "Fikk utslett av såpe",
                        Dato = new Dato { Ar = 2021, Maned = 7, Dag = 13 }
                    },
                    Pasient = new KosttilskuddPasientPart
                    {
                        Kjonn = PasientensKjonn.Mann,
                        Alder = 40
                    },
                },
                Melding = new KosttilskuddMeldingPart
                {
                    Bivirkning = new KosttilskuddBivirkningPart
                    {
                        BivirkningHvorPaKroppen = new List<string>()
                        {
                            HvorPaKroppen.Albue.ToString()
                        },
                        Reaksjon = new List<string>()
                        {
                            Reaksjon.EksemUtslett.ToString(),
                            Reaksjon.Hevelse.ToString()
                        },
                        FolgerAvBivirkning = FolgerAvBivirkning.Sykehusopphold.ToString()

                    },
                    RelevanteOpplysninger = new KosttilskuddRelevanteOpplysningerPart
                    {
                        BivirkningerVedTidligereBruk = BivirkningVedTidligereBruk.Ja,
                        TidligereBrukSpesifisert = null,
                        TilstandBerortOmradeForBruk = null,
                        AllergiEllerHudlidelse = null
                    },
                    Produkter = new List<KosttilskuddProduktPart>
                    {
                        new KosttilskuddProduktPart
                        {
                            Produktinformasjon = new KosttilskuddProduktinformasjon()
                            {
                                ProduktNavn = "Lano",
                                ProduktType = Produkttype.Harbehandling,
                                BatchLotNummer = "a",
                                Ingredienser = "Såpe",
                                LeverandorProdusent = "Lilleborg",
                                HvorProduktetErKjopt = KosttilskuddKjopssted.NetthandelNorge,
                                ErProduktUtgattPaHoldbarhetsdato = Utgatt.Nei
                            },
                            BrukAvProduktet = new KosttilskuddBrukAvProduktet()
                            {
                                AnbefaltDagligDose = "ja",
                                MengdeIngredienserEllerVirkestoffPrDagligDose = "1",
                                FaktiskInntattDagligDose = "1.5",
                            }
                        }
                    }
                }
            };

            // Example of data sent with request
            var jsonData = JsonConvert.SerializeObject(requestData, Formatting.Indented);
            Console.WriteLine(jsonData);
            Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}-------------------------------------------------{Environment.NewLine}{Environment.NewLine}");

            //call API, wait for response
            var apiClient = new EksternUonsketHendelseClient(httpClient);
            var response = await apiClient.KosttilskuddAsync(requestData);

            // If the call was succesful write out response
            if (response.Status == HttpStatusCode.OK || response.Status == HttpStatusCode.Created)
            {
                Console.WriteLine(response.Status == HttpStatusCode.OK
                    ? "Melding opprettet"
                    : "Melding oppdatert");

                Console.WriteLine("-- Responsdata");
                Console.WriteLine($"Melde.no ID: {response.Id}");
                Console.WriteLine($"EksterntSaksSystem: {response.EksterntSaksSystem}");
                Console.WriteLine($"EksternSaksid: {response.EksternSaksid}");
                Console.WriteLine($"MeldingsId: {response.MeldingsId}");
                Console.WriteLine($"Referansenummer: {response.Referansenummer}");
                Console.WriteLine($"MeldersEpost: {response.MeldersEpost}");
                Console.WriteLine("-- Mottakere");
                foreach (var m in response.Mottakere)
                {
                    Console.WriteLine($"\t{m}");
                }
            }
            else // Write out some error data if unsuccessful
            {
                Console.WriteLine("-- Feil");
                Console.WriteLine($"HTTP statuskode: {response.Status}");
                Console.WriteLine($"Correlation ID: {response.Feil?.CorrelationId}");
                Console.WriteLine("Feilemdling:");
                Console.WriteLine(response.Feil?.Message);
            }
        }
    }
}
