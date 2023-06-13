using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Example.Configuration;
using MeldeApiReport;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenAPI;

namespace Example.LegemiddelBivirkning
{
    class Program
    {
        static async Task Main()
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
                return new JwkTokenHandler(Config.HelseIdUrl, Config.ClientId, Config.Jwk, new[] { "nhn:melde/legemiddelbivirkning" }, Config.ClientType);
            });

            var provider = serviceCollection.BuildServiceProvider();
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            using var httpClient = httpClientFactory.CreateClient("MeldeNo");

            // Fill out request data
            var requestData = new DrugSideEffectsRequest
            {
                Header = new DrugSideEffectsHeaderPart
                {
                    ExternalCaseId = Guid.NewGuid().ToString(),
                    Reporter = new DrugSideEffectsReporterPart
                    {
                        Nin = "13065906141",
                        Organization = new OrganizationPart { OrgNumber = "883974832" },
                        Email = "TestData@melde.no",
                        Phone = "99999999"
                    },
                    Patient = new DrugSideEffectsPatientPart
                    {
                        Nin = "25868998388"
                    },
                    Incident = new DrugSideEffectsIncidentPart
                    {
                        IncidentDescription = "Det er mange opplysninger, men de har vi ikke."
                    }
                },
                Report = new DrugSideEffectsReportPart
                {
                    Drugs = new List<DrugSideEffectsDrugPart>
                    {
                        new DrugSideEffectsDrugPart
                        {
                            DrugId = "ID_7BC40EE4-9823-44BD-8785-A2F9B119EF5C",
                            Role = LegemiddelRolle.Samtidig,
                            Indication = "IndikasjonTestData",
                            StillInUse = YesNoDontKnow.No,
                            Dosage = new DrugSideEffectsDosagePart
                            {
                                DosageDescription = "1000mg per dag",
                                StartDate = "1990",
                                EndDate = "1994-05-24",
                                BatchLotNumber = "Batchnummer A412"
                            },
                            DrugName = "Painkillers",
                            IsVaccine = false
                        },
                        new DrugSideEffectsDrugPart
                        {
                            DrugId = "ID_7C25C265-6B8D-4E66-8832-D514BE638BF2",
                            Role = LegemiddelRolle.Mistenkt,
                            Indication = "",
                            StillInUse = YesNoDontKnow.Yes,
                            Dosage = new DrugSideEffectsDosagePart
                            {
                                StartDate = "1994-05-23",
                                VaccineTime = "23:05",
                                BatchLotNumber = "Batchnummer #R1195",
                                AdministrationRoute = AdministrationRoute.RightArm,
                                DoseNumber = DoseNumber.Two
                            },
                            DrugName = "Supervax",
                            IsVaccine = true
                        }
                    },
                    Symptoms = new List<DrugSideEffectsSymptomsPart>
                    {
                        new DrugSideEffectsSymptomsPart
                        {
                            Description = "Blodtrykksfall",
                            StartDate = "1994-05-24",
                            EndDate = "1994-05-25",
                            Ongoing = YesNo.No,
                        },
                        new DrugSideEffectsSymptomsPart
                        {
                            Description = "Stor trang til å teste APIer",
                            StartDate = "1994-05-24",
                            EndDate = "1994-05-25",
                            Ongoing = YesNo.No,
                            Outcome = SideEffectOutcome.RecoveredWithSymptoms
                        }
                    },
                    OtherInformation = new DrugSideEffectsOtherInformationPart
                    {
                        Severity = new List<SideEffectSeverity>
                        {
                            SideEffectSeverity.LifeThreatening,
                            SideEffectSeverity.AnomalyOrBirthDefect
                        }
                    }
                }
            };

            // Example of data sent with request
            var jsonData = JsonConvert.SerializeObject(requestData, Formatting.Indented);
            Console.WriteLine(jsonData);
            Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}-------------------------------------------------{Environment.NewLine}{Environment.NewLine}");

            // Call the API and wait for response
            var apiClient = new AdverseIncidentClient(httpClient);
            var response = await apiClient.LegemiddelBivirkningAsync(requestData);

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
