using System;
using System.Linq;
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
            // Example for Hendelsestype
            var nokupClient = new NokupClient(httpClient);

            // Start lookup and select hendelsestype (code 2)
            var nokupResult = await nokupClient.NokupLookupAsync("0");
            var selection = nokupResult.First().Options.First(n => n.Code == "2");

            // Lookup and select code 2.10
            nokupResult = await nokupClient.NokupLookupAsync(selection.Code);
            selection = nokupResult.First().Options.First(n => n.Code == "2.10");

            // Lookup and select code 2.10.1
            nokupResult = await nokupClient.NokupLookupAsync(selection.Code);
            selection = nokupResult.First().Options.First(n => n.Code == "2.10.1");

            // Lookup and select code 2.10.1.1
            nokupResult = await nokupClient.NokupLookupAsync(selection.Code);
            selection = nokupResult.First().Options.First(n => n.Code == "2.10.1.1");

            // Lookup and select code 2.10.1.1.1
            nokupResult = await nokupClient.NokupLookupAsync(selection.Code);
            selection = nokupResult.First().Options.First(n => n.Code == "2.10.1.1.1");

            nokupResult = await nokupClient.NokupLookupAsync(selection.Code);

            // set hendelsestype
            var hendelsestype = nokupResult.First().IsLeafNokupNode ? selection.Code : throw new Exception("This should be a leaf node");

            // Additional info
            string definisjonKode = "", oppdagetKode = "";

            foreach(var option in nokupResult.First().Options)
            {
                nokupResult = await nokupClient.NokupLookupAsync(option.Code);
                if (nokupResult.First().Code.Contains("Definisjon"))
                {
                    // Select DefinisjonKode = 3
                    definisjonKode = nokupResult.First().Options.Where(n => n.Code == (option.Code + ".3")).Select(n => n.Code).First().Split(".").Last();
                }
                else if (nokupResult.First().Code.Contains("Oppdaget"))
                {
                    // Select OppdagetKode = 2
                    oppdagetKode = nokupResult.First().Options.Where(n => n.Code == (option.Code + ".2")).Select(n => n.Code).First().Split(".").Last();
                }
            }

            //// Fill out request data
            var requestData = new BiovigilanceRequest
            {
                Header = new BiovigilansHeaderPart
                {
                    ExternalCaseId = Guid.NewGuid().ToString(),
                    Reporter = new BiovigilanceReporterPart
                    {
                        SSN = "13075706604",
                        Email = "TestData@melde.no",
                        OrganizationNumber = "883974832",
                    },
                    Incident= new BiovigilanceIncidentPart
                    {
                        IncidentDescription = "Datt på rattata",
                        IncidentDate = "2021-07-13"
                    },
                    Patient = new BiovigilancePatientPart
                    {
                        YearOfBirth = 1990,
                        Gender = Gender.Male,
                    },
                },
                Report = new BiovigilanceReportPart
                {
                    Biovigilance = new BiovigilancePart
                    {
                        StedForHendelsen = "1.1",
                        Hendelsestype = hendelsestype,
                        MedvirkendeFaktorer = new string[] { "3.2.1" },
                        Forebyggbarhet = "4.1",
                        FaktiskKonsekvensForPasient = "5.2",
                        Hyppighet = "6.2",
                        MuligKonsekvensVedGjentakelse = "7.3",
                        DefinisjonKode = definisjonKode,
                        OppdagetKode = oppdagetKode
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
