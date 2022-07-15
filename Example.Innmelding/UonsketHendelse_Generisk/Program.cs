using Example.Configuration;
using MeldeApiReport;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenAPI;

namespace Example.Varselordningen
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
                return new JwkTokenHandler(Config.HelseIdUrl, Config.ClientId, Config.Jwk, new[] { "nhn:melde/uonskethendelse" }, Config.ClientType);
            });

            var provider = serviceCollection.BuildServiceProvider();
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            using var httpClient = httpClientFactory.CreateClient("MeldeNo");

            //// Fill out request data
            var requestData = new UonsketHendelseRequest
            {
                Hode = new UonsketHendelseHodePart
                {
                    EksternSaksId = Guid.NewGuid().ToString(),
                    Melder = new UonsketHendelseMelderPart
                    {
                        Fødselsnummer = "13075706604",
                        Epost = "TestData@melde.no",
                        Telefon = "99999999",
                        Organisasjonsnummer = "883974832",
                        Rolle = MelderRolle.Behandler,
                        Stilling = "Lege"
                    },
                    Hendelse = new UonsketHendelseHendelsePart
                    {
                        HvaSkjedde = "Datt på rattata",
                        Dato = "2021-07-13"
                    },
                    Pasient = new UonsketHendelsePasientPart
                    {
                        //Fødselsdato = new Dato { Ar = 1990, Maned = 7, Dag = 13 },
                        //Kjønn = PasientensKjonn.Mann,
                        Fødselsnummer = "13075706604"
                    },
                    Meldingstyper = new UonsketHendelseMeldingstyperPart
                    {
                        AlvorligHendelse = true,
                        Kosttilskudd = true
                    },
                },
                Meldinger = new UonsketHendelseMeldingerPart
                {
                    AlvorligHendelse = new AlvorligHendelseMeldingPart
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
                            VarsletTilStatsforvalter = YesNoDontKnow.Ja,
                            VarsletStatsforvalter = "Fylkesmannen i Viken",
                        }
                    },
                    Kosttilskudd = new KosttilskuddMeldingPart
                    {
                        Bivirkning = new KosttilskuddBivirkningPart
                        {
                            Reaksjoner = new List<string>()
                        {
                            Reaksjon.EksemUtslett.ToString(),
                            Reaksjon.Hevelse.ToString()
                        },
                            Reaksjonstid = "ReakTid",
                            ReaksjonstidTekst = "ReakTidTekst",
                            FolgerAvBivirkning = "Folger",
                            FolgerAvBivirkningTekst = "FolgerTekst",
                            BivirkningVarighet = "BivVarighet",
                            PaagaarFortsatt = YesNo.Ja,
                            BivirkningerVedTidligereBruk = YesNoDontKnow.VetIkke,
                            BivirkningerVedTidligereBrukTekst = "BivTidBrukTekst",
                            AllergiEllerAnnenPaavirkendeFaktor = YesNoDontKnow.Ja,
                            AllergiEllerAnnenPaavirkendeFaktorTekst = "AllergiTekst",
                            HarUnderliggendeSykdom = YesNoDontKnow.Ja,
                            KanUnderliggendeSykdomVareArsak = YesNoDontKnow.Nei
                        },
                        Produkter = new[]
                        {
                            new KosttilskuddProduktPart
                            {
                                Produktinformasjon = new KosttilskuddProduktinformasjonPart
                                {
                                    ProduktNavn = "ProdNavn",
                                    Ingredienser = "Ingr",
                                    LeverandorProdusent = "LevProdusent",
                                    Holdbarhetsdato = "2022-12-02",
                                    ErBivirkningMeldtTilLeverandorEllerProdusent = YesNoDontKnow.VetIkke,
                                    HvorProduktetErKjopt = "Hvor",
                                    ButikkNavn = "Butikknavn",
                                    BatchLotNummer = "Batch",
                                    Vedlegg = new []
                                    {
                                        new Vedlegg
                                        {
                                            Navn = "Test.txt",
                                            Innhold = "SGVyIGVyIGV0IGVua2VsdCB0ZXN0dmVkbGVnZy4NCg=="
                                        }
                                    }
                                },
                                BrukAvProduktet = new KosttilskuddBrukAvProduktetPart
                                {
                                    MengdeIngredienserEllerVirkestoffPrDagligDose = "Mengde",
                                    AnbefaltDagligDose = "DglDose",
                                    FaktiskInntattDagligDose = "FaktiskDose",
                                    BruksPeriode = "Periode",
                                    LegemidlerSamtidigSomKosttilskudd = YesNoDontKnow.Ja,
                                    HvilkeLegemiderErTattSamtidig = new [] { "Dundersalt" },

                                }
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
            var apiClient = new EksternUonsketHendelseClient(httpClient);
            var response = await apiClient.UonsketHendelseAsync(requestData);

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
