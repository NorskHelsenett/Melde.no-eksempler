using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Example.Configuration;
using MeldeApiReport;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenAPI;

namespace Example.AlvorligHendelse
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
                return new JwkTokenHandler(Config.HelseIdUrl, Config.ClientId, Config.Jwk, new[] { "nhn:melde/alvorlighendelse" }, Config.ClientType);
            });

            var provider = serviceCollection.BuildServiceProvider();
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            using var httpClient = httpClientFactory.CreateClient("MeldeNo");

            //// Fill out request data
            var requestData = new SeriousIncidentRequest
            {
                Header = new SeriousIncidentHeaderPart
                {
                    ExternalCaseId = Guid.NewGuid().ToString(),
                    Reporter = new SeriousIncidentReporterPart
                    {
                        SSN = "13075706604",
                        Email = "TestData@melde.no",
                        Phone = "99999999",
                        OrganizationNumber = "883974832",
                        Role = ReporterRole.Treator,
                        Position = "Lege"
                    },
                    Incident = new SeriousIncidentIncidentPart
                    {
                        IncidentDescription = "Datt på rattata",
                        IncidentDate = "2021-07-13"
                    },
                    Patient = new SeriousIncidentPatientPart
                    {
                        //DateOfBirth = "1990-07-13",
                        //Gender = Gender.Male,
                        SSN = "13075706604",
                    },
                },
                Report = new SeriousIncidentReportPart
                {
                    ContactPersons = new List<SeriousIncidentContactPerson>
                    {
                        new SeriousIncidentContactPerson
                        {
                            Name = "VILDE MOEN_BRATLI",
                            Email = "TestData@melde.no",
                            Phone = "00000000",
                            Position = "Doktor"
                        }
                    },
                    NextOfKin = new List<SeriousIncidentNextOfKin>
                    {
                        new SeriousIncidentNextOfKin
                        {
                            Name = "Bror Børresen"
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
