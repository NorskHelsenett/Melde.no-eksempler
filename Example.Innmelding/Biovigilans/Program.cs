using System;
using System.Net.Http;
using System.Threading.Tasks;
using Example.Configuration;
using MeldeApiReport;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenAPI;

namespace Example.Biovigilans
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
                return new JwkTokenHandler(Config.HelseIdUrl, Config.ClientId, Config.Jwk, new[] { "nhn:melde/biovigilans" }, Config.ClientType);
            });

            var provider = serviceCollection.BuildServiceProvider();
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            using var httpClient = httpClientFactory.CreateClient("MeldeNo");

            // Nokup lookup
            var nokupClient = new NokupClient(httpClient);
            var nokupResult = await nokupClient.NokupLookupAsync("0");

            //// Fill out request data
            var requestData = new BiovigilansRequest
            {
                Hode = new HodePartOfBiovigilansMelderPartAndBiovigilansHendelsePartAndBiovigilansPasientPart
                {
                    EksternSaksId = Guid.NewGuid().ToString(),
                    Melder = new BiovigilansMelderPart
                    {
                        Fødselsnummer = "13075706604",
                        Epost = "TestData@melde.no",
                        Organisasjonsnummer = "883974832",
                    },
                    Hendelse = new BiovigilansHendelsePart
                    {
                        HvaSkjedde = "Datt på rattata",
                        Dato = new Dato { Ar = 2021, Maned = 7, Dag = 13 }
                    },
                    Pasient = new BiovigilansPasientPart
                    {
                        Fødselsår = 1990,
                        Kjønn = PasientensKjonn.Mann,
                    },
                },
                Melding = new BiovigilansMeldingPart
                {
                    Biovigilans = new BiovigilansPart
                    {
                        StedForHendelsen = "1.1",
                        Hendelsestype = "2.10.1.1.1",
                        MedvirkendeFaktorer = new string[] { "3.2.1" },
                        Forebyggbarhet = "4.1",
                        FaktiskKonsekvensForPasient = "5.2",
                        Hyppighet = "6.2",
                        MuligKonsekvensVedGjentakelse = "7.3",
                        DefinisjonKode = "2",
                        OppdagetKode = "1"
                    }
                }
            };

            // Example of data sent with request
            var jsonData = JsonConvert.SerializeObject(requestData, Formatting.Indented);
            Console.WriteLine(jsonData);
            Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}-------------------------------------------------{Environment.NewLine}{Environment.NewLine}");

            //call API, wait for response
            var apiClient = new EksternUonsketHendelseClient(httpClient);
            var response = await apiClient.BiovigilansAsync(requestData);

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
