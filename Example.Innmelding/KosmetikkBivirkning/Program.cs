using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Example.Configuration;
using MeldeApiReport;
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
                return new JwkTokenHandler(Config.HelseIdUrl, Config.ClientId, Config.Jwk, new[] { "nhn:melde/kosmetikk" }, Config.ClientType);
            });

            var provider = serviceCollection.BuildServiceProvider();
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            using var httpClient = httpClientFactory.CreateClient("MeldeNo");

            //// Fill out request data
            var requestData = new KosmetikkRequest
            {
                Hode = new HodePartOfKosmetikkMelderPartAndKosmetikkHendelsePartAndKosmetikkPasientPart
                {
                    EksternSaksId = Guid.NewGuid().ToString(),
                    Melder = new KosmetikkMelderPart
                    {
                        Fødselsnummer = "13075706604",
                        Epost = "TestData@melde.no",
                        Organisasjonsnummer = "883974832"
                    },
                    Hendelse = new KosmetikkHendelsePart
                    {
                        HvaSkjedde = "Fikk utslett av såpe",
                        Dato = new Dato { Ar = 2021, Maned = 7, Dag = 13 }
                    },
                    Pasient = new KosmetikkPasientPart
                    {
                        Kjønn = PasientensKjonn.Mann,
                        Alder = 40
                    },
                },
                Melding = new KosmetikkMeldingPart
                {
                    Bivirkning = new KosmetikkBivirkningPart
                    {
                        BivirkningHvorPaKroppen = new List<HvorPaKroppen>
                        {
                            HvorPaKroppen.Ansikt,
                            HvorPaKroppen.Mage,
                        },
                        Reaksjon = new List<Reaksjon>
                        {
                            Reaksjon.EksemUtslett,
                            Reaksjon.Hevelse
                        },
                        Reaksjonstid = Reaksjonstid.Innen30Min,
                    },
                    RelevanteOpplysninger = new KosmetikkRelevanteOpplysningerPart
                    {
                        BivirkningerVedTidligereBruk = BivirkningVedTidligereBruk.Delvis
                    },
                    Produkter = new List<KosmetikkProduktPart>
                    {
                        new KosmetikkProduktPart
                        {
                            Produktinformasjon = new KosmetikkProduktinformasjonPart
                            {
                                ProduktNavn = "Lano",
                                ProduktType = Produkttype.Sape,
                                Salgskanal = Salgskanal.Matvarebutikk
                            },
                            BrukAvProduktet = new KosmetikkBrukAvProduktetPart
                            {
                                ProduktetBruktHvorPaKroppen = new List<HvorPaKroppen>
                                {
                                    HvorPaKroppen.Ansikt,
                                    HvorPaKroppen.Mage
                                },
                                BeskrivelseAvBruk = "Vårrengjøring"
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
            var response = await apiClient.KosmetikkAsync(requestData);

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
