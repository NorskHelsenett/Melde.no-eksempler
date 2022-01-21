using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using MeldeApi;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenAPIs;

namespace Example.LegemiddelBivirkning
{
    class Program
    {
        // Points to Melde.no API base
        //private static readonly Uri ApiBaseAddress = new ("https://localhost:44342/");
        private static readonly Uri ApiBaseAddress = new("https://api.test.melde.no/");

        // Points to the HelseId instance you want to use
        private static readonly string HelseIdUrl = "https://helseid-sts.test.nhn.no";

        static async Task Main()
        {
            // Setup HTTP client with authentication for HelseId
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("MeldeNo", client =>
                {
                    client.BaseAddress = ApiBaseAddress;
                })
                .AddHttpMessageHandler(_ =>
                {
                    // Provide your own client id and private key settings
                    var clientId = "<client id>";
                    var jwtPrivateKey = new Dictionary<string, object>
                    {
                        // ... key parts
                    };

                    return new JwkTokenHandler(HelseIdUrl, clientId, jwtPrivateKey, ClientType.Machine);
                });

            var provider = serviceCollection.BuildServiceProvider();
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            using var httpClient = httpClientFactory.CreateClient("MeldeNo");

            // Fill out request data
            var requestData = new LegemiddelBivirkningRequest
            {
                EksternSaksId = "MYSYS-R195",
                Melder = new MelderPart
                {
                    Fødselsnummer = "13065906141",
                    Organisasjonsnummer = "883974832",
                    Epost = "TestData@melde.no",
                    Telefon = "99999999"
                },
                Pasient = new PasientPart
                {
                    Fødselsdato = "07.03.1990",
                    Kjønn = PasientensKjonn.Mann
                },
                Legemidler = new List<LegemidlerPart>
                {
                    new LegemidlerPart
                    {
                        MerkevareId = "ID_7BC40EE4-9823-44BD-8785-A2F9B119EF5C",
                        Rolle = LegemiddelRolle.Samtidig,
                        Indikasjon = "IndikasjonTestData",
                        ErPagaendeBehandling = PagaendeBehandling.Nei,
                        Dosering = new LegemiddelDoseringPart
                        {
                            Doseringstekst = "1000mg per dag",
                            Startdato = new Dato {Ar = 1990},
                            Sluttdato = new Dato {Ar = 1994, Maned = 5, Dag = 24},
                            Batchnummer = "Batchnummer A412"
                        },
                        PreparatNavn = "Painkillers",
                        ErVaksine = false
                    },
                    new LegemidlerPart
                    {
                        MerkevareId = "ID_7C25C265-6B8D-4E66-8832-D514BE638BF2",
                        Rolle = LegemiddelRolle.Mistenkt,
                        Indikasjon = "",
                        ErPagaendeBehandling = PagaendeBehandling.Ja,
                        Dosering = new LegemiddelDoseringPart
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
                Symptom = new List<SymptomPart>
                {
                    new SymptomPart
                    {
                        Beskrivelse = "Blodtrykksfall",
                        Startdato = new Dato {Ar = 1994, Maned = 5, Dag = 24},
                        Sluttdato = new Dato {Ar = 1994, Maned = 5, Dag = 25},
                        PågårFortsatt = PågårFortsatt.Nei,
                    },
                    new SymptomPart
                    {
                        Beskrivelse = "Stor trang til å teste APIer",
                        Startdato = new Dato {Ar = 1994, Maned = 5, Dag = 24},
                        Sluttdato = new Dato {Ar = 1994, Maned = 5, Dag = 25},
                        PågårFortsatt = PågårFortsatt.Nei,
                        Utfall = BivirkningUtfall.RestituertMenMedEttervirkninger
                    }
                },
                AnnenInformasjon = new AnnenInformasjonPart
                {
                    Alvorlighet = new List<BivirkningAlvorlighet>
                        {BivirkningAlvorlighet.Livstruende, BivirkningAlvorlighet.AnomaliFodselsdefekt},
                    AndreOpplysninger = "Det er mange opplysninger, men de har vi ikke."
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
