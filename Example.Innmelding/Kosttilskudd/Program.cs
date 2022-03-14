﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using MeldeApi;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenAPI;

namespace Example.Kosmetikk
{
    class Program
    {
        // Points to Melde.no API base
        private static readonly Uri ApiBaseAddress = new("https://localhost:44342/");
        // private static readonly Uri ApiBaseAddress = new("https://api.test.melde.no/");

        // Points to the HelseId instance you want to use
        private static readonly string HelseIdUrl = "https://helseid-sts.test.nhn.no";


        static async Task Main(string[] args)
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
                    var clientType = ClientType.Machine;
                    var clientId = "<client id>";
                    var jwtPrivateKey = new Dictionary<string, object>
                    {
                        // ... key parts
                    };

                    return new JwkTokenHandler(HelseIdUrl, clientId, jwtPrivateKey, new string[] { "nhn:melde/kosttilskudd" }, clientType);
                });

            var provider = serviceCollection.BuildServiceProvider();
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            using var httpClient = httpClientFactory.CreateClient("MeldeNo");

            //// Fill out request data
            var requestData = new KosttilskuddRequest()
            {
                EksternSaksId = "MYSYS-R195",
                Melder = new Melder
                {
                    Navn = "VILDE MOEN-BRATLI",
                    Fodselsnummer = "13075706604",
                    Epost = "TestData@melde.no",
                    Telefon = "99999999",
                    HerId = 0,
                    Stilling = "Doktor",
                    Rolle = MelderRolle.Behandler,
                    HprId = 8458111,
                    Virksomhet = new Virksomhet
                    {
                        Navn = "ST. OLAVS HOSPITAL HF",
                        Helseregion = "ST. OLAVS HOSPITAL HF",
                        Postadresse = "Prinsesse Kristinas gate 3",
                        Postnummer = "7030",
                        Poststed = "TRONDHEIM",
                        Kommune = "TRONDHEIM",
                        Orgnummer = "883974832",
                        Naringskode = new Naringskode
                        {
                            Id = "86.101",
                            Navn = "Alminnelige somatiske sykehus",
                        }
                    },
                    GjeldendeVirksomhet = new Virksomhet
                    {
                        Navn = "ST. OLAVS HOSPITAL HF",
                        Helseregion = "ST. OLAVS HOSPITAL HF",
                        Postadresse = "Prinsesse Kristinas gate 3",
                        Postnummer = "7030",
                        Poststed = "TRONDHEIM",
                        Kommune = "TRONDHEIM",
                        Orgnummer = "883974832",
                        Naringskode = new Naringskode
                        {
                            Id = "86.101",
                            Navn = "Alminnelige somatiske sykehus",
                        }
                    }
                },
                Hendelse = new Hendelse
                {
                    HvaSkjedde = "Fikk utslett av såpe",
                    Tidspunkt = new Tidspunkt
                    {
                        DatoForHendelsen = new Dato { Ar = 2021, Maned = 7, Dag = 13 },
                    }
                },
                Pasient = new Pasienten
                {
                    Kjonn = PasientensKjonn.Mann,
                    Alder = 40
                },
                Bivirkning = new KosttilskuddBivirkning()
                {
                    BivirkningHvorPaKroppen = new List<string>()
                    {
                        HvorPaKroppen.Albue.ToString()
                    },
                    Reaksjon = new List<string>()
                    {
                        Reaksjon.EksemUtslett.ToString(),
                        Reaksjon.Hevelse.ToString()
                    },
                    FolgerAvBivirkning = FolgerAvBivirkning.Sykehusopphold.ToString()

                },
                RelevanteOpplysninger = new RelevanteOpplysninger
                {
                    BivirkningerVedTidligereBruk = BivirkningVedTidligereBruk2.Ja,
                    TidligereBrukSpesifisert = null,
                    TilstandBerortOmradeForBruk = null,
                    AllergiEllerHudlidelse = null
                },
                Produkter = new List<KosttilskuddProdukt>
                {
                    new KosttilskuddProdukt()
                    {
                        Produktinformasjon = new Produktinformasjon()
                        {
                            ProduktNavn = "Lano",
                            ProduktType = Produkttype.Sape,
                            BatchLotNummer = "a"
                        },
                        BrukAvProduktet = new BrukAvProduktet()
                        {
                            AnbefaltDagligDose = "ja"
                        }
                    }
                },
                Sprak = Language.Engelsk

            };

            // Example of data sent with request
            var jsonData = JsonConvert.SerializeObject(requestData, Formatting.Indented);
            Console.WriteLine(jsonData);
            Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}-------------------------------------------------{Environment.NewLine}{Environment.NewLine}");

            //call API, wait for response
            var apiClient = new EksternUonsketHendelseClient(httpClient);
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