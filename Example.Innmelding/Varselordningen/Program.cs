using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Example.Configuration;
using MeldeApi;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenAPI;

namespace Example.Varselordningen
{
    class Program
    {
        // Points to the HelseId instance you want to use
        private static readonly string HelseIdUrl = "https://helseid-sts.test.nhn.no";

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
                return new JwkTokenHandler(HelseIdUrl, Config.ClientId, Config.Jwk, new[] { "nhn:melde/alvorlighendelse" }, Config.ClientType);
            });

            var provider = serviceCollection.BuildServiceProvider();
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            using var httpClient = httpClientFactory.CreateClient("MeldeNo");

            //// Fill out request data
            var requestData = new AlvorligHendelseRequest
            {
                Hode = new HodePartOfAlvorligHendelseMelderPartAndAlvorligHendelseHendelsePartAndAlvorligHendelsePasientPart
                {
                    EksternSaksId = Guid.NewGuid().ToString(),
                    Melder = new AlvorligHendelseMelderPart
                    {
                        Fødselsnummer = "130757066049",
                        Epost = "TestData@melde.no",
                        Telefon = "99999999",
                        Organisasjonsnummer = "883974832",
                        Organisasjonsnavn = "St. Olavs Hospital",
                        Rolle = MelderRolle.Behandler,
                        Stilling = "Lege"
                    },
                    Hendelse = new AlvorligHendelseHendelsePart
                    {
                        HvaSkjedde = "Datt på rattata",
                        Dato = new Dato { Ar = 2021, Maned = 7, Dag = 13 }
                    },
                    Pasient = new AlvorligHendelsePasientPart
                    {
                        //Fødselsdato = new Dato { Ar = 1990, Maned = 7, Dag = 13 },
                        //Kjønn = PasientensKjonn.Mann,
                        Fødselsnummer = "13075706604"
                    },
                },
                Melding = new AlvorligHendelseMeldingPart
                {
                    Kontaktpersoner = new List<AlvorligHendelseKontaktperson>
                {
                    new AlvorligHendelseKontaktperson
                    {
                        Navn = "VILDE MOEN_BRATLI",
                        Epost = "TestData@melde.no",
                        Telefon = "00000000",
                        Stilling = "Doktor"
                    }
                },
                    AnnenInformasjon = new AlvorligHendelseAnnenInformasjon
                    {
                        VarsletTilFylkesmannen = true,
                        VarsletFylkesmann = "Fylkesmannen i Viken",
                        TidligereVarslet = true,
                        TidligereVarsletKanal = "Faks til sykehus"
                    }
                }
            };

            // Example of data sent with request
            var jsonData = JsonConvert.SerializeObject(requestData, Formatting.Indented);
            Console.WriteLine(jsonData);
            Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}-------------------------------------------------{Environment.NewLine}{Environment.NewLine}");

            //call API, wait for response
            var apiClient = new EksternUonsketHendelseClient(httpClient);
            var response = await apiClient.AlvorligHendelseAsync(requestData);

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
