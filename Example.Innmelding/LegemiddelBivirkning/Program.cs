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
            var requestData = new LegemiddelBivirkningRequest
            {
                Hode = new HodePartOfLegemiddelBivirkningMelderPartAndLegemiddelBivirkningHendelsePartAndLegemiddelBivirkningPasientPart
                {
                    EksternSaksId = Guid.NewGuid().ToString(),
                    Melder = new LegemiddelBivirkningMelderPart
                    {
                        Fødselsnummer = "13065906141",
                        Organisasjonsnummer = "883974832",
                        Epost = "TestData@melde.no",
                        Telefon = "99999999"
                    },
                    Pasient = new LegemiddelBivirkningPasientPart
                    {
                        Fødselsdato = new Dato { Ar = 1990, Maned = 3, Dag = 7 },
                        Kjønn = PasientensKjonn.Mann
                    },
                    Hendelse = new LegemiddelBivirkningHendelsePart
                    {
                        HvaSkjedde = "Det er mange opplysninger, men de har vi ikke."
                    }
                },
                Melding = new LegemiddelBivirkningMeldingPart
                {
                    Legemidler = new List<LegemiddelBivirkningLegemidlerPart>
                    {
                        new LegemiddelBivirkningLegemidlerPart
                        {
                            MerkevareId = "ID_7BC40EE4-9823-44BD-8785-A2F9B119EF5C",
                            Rolle = LegemiddelRolle.Samtidig,
                            Indikasjon = "IndikasjonTestData",
                            ErPagaendeBehandling = YesNoDontKnow.Nei,
                            Dosering = new LegemiddelBivirkningLegemiddelDoseringPart
                            {
                                Doseringstekst = "1000mg per dag",
                                Startdato = new Dato {Ar = 1990},
                                Sluttdato = new Dato {Ar = 1994, Maned = 5, Dag = 24},
                                Batchnummer = "Batchnummer A412"
                            },
                            PreparatNavn = "Painkillers",
                            ErVaksine = false
                        },
                        new LegemiddelBivirkningLegemidlerPart
                        {
                            MerkevareId = "ID_7C25C265-6B8D-4E66-8832-D514BE638BF2",
                            Rolle = LegemiddelRolle.Mistenkt,
                            Indikasjon = "",
                            ErPagaendeBehandling = YesNoDontKnow.Ja,
                            Dosering = new LegemiddelBivirkningLegemiddelDoseringPart
                            {
                                Startdato = new Dato {Ar = 1994, Maned = 5, Dag = 23},
                                Tidspunkt = new Klokkeslett {Timer = 23, Minutter = 5},
                                Batchnummer = "Batchnummer #R1195",
                                Administrasjonssted = LegemiddelAdministrasjonssted.HoyreArm,
                                Dosenummer = Dosenummer.To
                            },
                            PreparatNavn = "Supervax",
                            ErVaksine = true
                        }
                    },
                    Symptom = new List<LegemiddelBivirkningSymptomPart>
                    {
                        new LegemiddelBivirkningSymptomPart
                        {
                            Beskrivelse = "Blodtrykksfall",
                            Startdato = new Dato {Ar = 1994, Maned = 5, Dag = 24},
                            Sluttdato = new Dato {Ar = 1994, Maned = 5, Dag = 25},
                            PågårFortsatt = YesNo.Nei,
                        },
                        new LegemiddelBivirkningSymptomPart
                        {
                            Beskrivelse = "Stor trang til å teste APIer",
                            Startdato = new Dato {Ar = 1994, Maned = 5, Dag = 24},
                            Sluttdato = new Dato {Ar = 1994, Maned = 5, Dag = 25},
                            PågårFortsatt = YesNo.Nei,
                            Utfall = BivirkningUtfall.RestituertMenMedEttervirkninger
                        }
                    },
                    AnnenInformasjon = new LegemiddelBivirkningAnnenInformasjonPart
                    {
                        Alvorlighet = new List<BivirkningAlvorlighet>
                        {BivirkningAlvorlighet.Livstruende, BivirkningAlvorlighet.AnomaliFodselsdefekt},
                    }
                }
            };

            // Example of data sent with request
            var jsonData = JsonConvert.SerializeObject(requestData, Formatting.Indented);
            Console.WriteLine(jsonData);
            Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}-------------------------------------------------{Environment.NewLine}{Environment.NewLine}");

            // Call the API and wait for response
            var apiClient = new EksternUonsketHendelseClient(httpClient);
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
