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
            var requestData = new CosmeticsRequest
            {
                Header = new CosmeticsHeaderPart
                {
                    ExternalCaseId = Guid.NewGuid().ToString(),
                    Reporter = new CosmeticsReporterPart
                    {
                        SSN = "13075706604",
                        Email = "TestData@melde.no",
                        Phone = "99887766",
                        OrganizationNumber = "883974832",
                        
                    },
                    Incident = new CosmeticsIncidentPart
                    {
                        IncidentDescription = "Fikk utslett av såpe",
                    },
                    Patient = new CosmeticsPatientPart
                    {
                        Gender = Gender.Male,
                        YearOfBirth = 1982
                    },
                },
                Report = new CosmeticsReportPart
                {
                    SideEffects = new CosmeticsSideEffectsPart
                    {
                        AffectedSkinAreas = new List<BodyLocation>
                        {
                            BodyLocation.Face,
                            BodyLocation.Stomach,
                        },
                        Reactions = new List<Reaction>
                        {
                            Reaction.EczemaRash,
                            Reaction.Swelling
                        },
                        ReactionDelay = ReactionDelay.WithinHalfHour,
                    },
                    RelevantInformation = new CosmeticsRelevantInformationPart
                    {
                        SideEffectsOnPreviousUse = YesNoPartialDontKnow.Partial
                    },
                    Products = new List<CosmeticsProductPart>
                    {
                        new CosmeticsProductPart
                        {
                            ProductInformation = new CosmeticsProductInformationPart
                            {
                                ProductName = "Lano",
                                ProductType = Produkttype.Sape,
                                SalesChannel = SalesChannel.GroceryStore
                            },
                            ProductUsage = new CosmeticsProductUsagePart
                            {
                                BodyLocations = new List<BodyLocation>
                                {
                                    BodyLocation.Face,
                                    BodyLocation.Stomach
                                },
                                UsageDescription = "Vårrengjøring"
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
            var apiClient = new AdverseIncidentClient(httpClient);
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
