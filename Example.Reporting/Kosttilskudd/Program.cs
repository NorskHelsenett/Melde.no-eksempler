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
                return new JwkTokenHandler(Config.HelseIdUrl, Config.ClientId, Config.Jwk, new[] { "nhn:melde/kosttilskudd" }, Config.ClientType);
            });

            var provider = serviceCollection.BuildServiceProvider();
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            using var httpClient = httpClientFactory.CreateClient("MeldeNo");

            //// Fill out request data
            var requestData = new DietarySupplementsRequest()
            {
                Header = new DietarySupplementsHeaderPart
                {
                    ExternalCaseId = Guid.NewGuid().ToString(),
                    Reporter = new DietarySupplementsReporterPart
                    {
                        Nin = "13075706604",
                        Email = "TestData@melde.no",
                        Phone = "99887766",
                        Organization = new OrganizationPart { OrgNumber = "883974832" },
                    },
                    Incident = new DietarySupplementsIncidentPart
                    {
                        IncidentDescription = "Fikk utslett av såpe",
                    },
                    Patient = new DietarySupplementsPatientPart
                    {
                        Gender = Gender.Male,
                        YearOfBirth = 1982
                    },
                },
                Report = new DietarySupplementsReportPart
                {
                    SideEffect = new DietarySupplementsSideEffectsPart
                    {
                        Reactions = new List<string>()
                        {
                            "EczemaRash",
                            "Swelling"
                        },
                        ReactionDelay = "ReakTid",
                        ReactionDelayDescription = "ReakTidTekst",
                        Outcome = "Folger",
                        OutcomeDescription = "FolgerTekst",
                        SideEffectDuration = "BivVarighet",
                        Ongoing = YesNo.Yes,
                        SideEffectsOnPreviousUse = YesNoDontKnow.DontKnow,
                        SideEffectsOnPreviousUseDescription = "BivTidBrukTekst",
                        AnyAllergiesOrInfluencingIssues = YesNoDontKnow.Yes,
                        AnyAllergiesOrInfluencingIssuesDescription = "AllergiTekst",
                        AnyUnderlyingDiseases = YesNoDontKnow.Yes,
                        CanDiseaseBeTheCause = YesNoDontKnow.No
                    },
                    Products = new[]
                    {
                        new DietarySupplementsProductPart
                        {
                            ProductInformation = new DietarySupplementsProductInformationPart
                            {
                                ProductName = "ProdNavn",
                                Ingredients = "Ingr",
                                VendorOrManufacturer = "LevProdusent",
                                BestBeforeDate = "2022-12-02",
                                ReportedToVendorOrManufacturer = YesNoDontKnow.DontKnow,
                                PurchaseLocation = "Hvor",
                                ShopName = "Butikknavn",
                                BatchLotNumber = "Batch",
                                Attachments = new []
                                {
                                    new AttachmentPart
                                    {
                                        Name = "Test.txt",
                                        Content = "SGVyIGVyIGV0IGVua2VsdCB0ZXN0dmVkbGVnZy4NCg=="
                                    }
                                }
                            },
                            ProductUsage = new DietarySupplementsProductUsagePart
                            {
                                AmountOfIngredientsInDailyDose = "Mengde",
                                RecommendedDailyDose = "DglDose",
                                ActualDailyDoseTaken = "FaktiskDose",
                                UsageDuration = "Periode",
                                AnyDrugsTakenAtSameTime = YesNoDontKnow.Yes,
                                DrugsTakenAtSameTime = new [] { "Dundersalt" },

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
