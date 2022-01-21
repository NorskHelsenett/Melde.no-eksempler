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
        private static readonly Uri ApiBaseAddress = new ("https://localhost:44342/");
        //private static readonly Uri ApiBaseAddress = new("https://api.test.melde.no/");

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
                    var clientId = "511115ba-a407-4b08-9701-0a1bb247fb26";
                    var jwtPrivateKey = new Dictionary<string, object>
{
    {"kty", "RSA"},
    {"kid", "SA94ZyNwVIawIdJZKywWx1hp45gXuA8H3H9goSfMJRk"},
    {"use", "sig"},
    {"n", "zywzq64SFxScuIZMp93ATLvbsnCIwjheBb6r7jp4rGA5aRCs0U5OIbCRPc1SXsezAnsM8SVUVpB51mIYNMyRrq38dHnUl6KQWpPlyTIf7xUAVRJkJgI1VvYrMVfmErykWe9_1J89iLZjgKanmUft1HnlBHAjWZok89JBiY1KhuwiPXef3x_0KZOmZVvQv1aNAN3cSkV8rJhVgLGl_VUotlbetGYfNc9D3zJDy2-6TX7aM8seyO-mevDDmjC1Y_de7ArCUWZ50prtR_k8ygZez2g1MA0H2nnceyQtB3NosoAqOjgL8ayRjo_n-CGOErdE4gjQpuJL8lkCV3zzW5E-HQ"},
    {"e", "AQAB"},
    {"d", "FRtmxEX-19UkxnQAWVXxYp_9GSf39vmxMpqjf6j7ZGyFTNwDD2wP78TCd250xu1HoqgQwHzSI-OiViI2XyK8cPSO9Pr4mt5YILJSxfXSZRRZrVErXOf6sTpxWhyfdyc7A2KwPmRe64_RgWj5SFeYtn4YxCP8pgNbYm_4d2AqyKYVPYiAk1mVuP7g6lApqXi8R93f20Gtbit77QnGQayVav7i0wODBVDv2I-NUlf0VoNOVWooTJGlnhYcM4MJ-vq18Oxb-7pXw_HYUjUqmCohQZ2_Jiicx5P8nwZ8lVtpUAQVP18sMDpcFxxqT640RnjBWpjVoCtPaNC-rcm4hvJVQQ"},
    {"p", "7v-xrpO41Zq2QtXCOh-vR5qVIAfDWE5W_fChVJmVrGHIaaboWUHCmnv7SWSFVsQqLMIsShZSQ0rdKWLT98Ltki3CqnovsAo2c7DU9o_hPg2Yyc_drz-srMOs9VXVPrKXzvrX86oICtCLJntGc5AZkwooQunDGJFx4wBcQGIO7a0"},
    {"q", "3ejvdhLYJQPvETyDUD6PUVa2PyZE7JaoUjagbchl6yaTEKSAfn47AltgxhwGnqPC1fsDo6HcrjsDmnX2lSQFgjkm0Sz4KL3Jff9e-zhkVd5V44nonDEbApbfQCJ-H78O_l6Ht69BmHADtFVOlkk4D37reOGf40e-2IsQWpzEwDE"},
    {"dp", "qg2HOKdlQZ9GhgGgpEi9J96mstazONbs8NzRfeeV5sTgm8Ql2LSAqfkDkHIUqesD8zrp1oFRYQ4YlQT7u3OYJIWo2DH-UmzB18l_jjxL0SJNj3L20mYlD_xeyWWcSHM8rwous_JMrJ08FVJri3iBqez1Pr8jkQyUEyfDfBAJfTU"},
    {"dq", "kTTPHLAQB4ifcuPp-SQ2m77l2kSsbTPYSJO-PpgXONww937tJdhrvIsWtAu6uSvnXiW2p-hOgyPSo8v04nqDsEa9g7qtV9t4cZ4dBL5NyXKHOTEQqMPpLLSUuV7YsOVQZlps7GEdxyXlBqebmPOoX1tpsdvRx-M2mnPE68YRaME"},
    {"qi", "n57mge6QqmAUuKv8ZlVxGCy270zuBNTV0vA9QNDH6-i8Fpcfe9f43bkR0nWoGKagdOMv6_0YGeb_5Arh04GrsMCG5Dh2n4xMlv8LLoJsYMVRkUjxXlfHEA83SYzVNZWOcmmuQ9A3WCOvuqgat1ePqM3kl4xRnU8mJxpUNhZ0ITg"}
};

                    var clientIdPers = "04819e7f-ee6e-429a-b0c9-6a835c648dc0";
                    var jwtPrivateKeyPers = new Dictionary<string, object>
                    {
                        {"d", "H_prYmKbySa17OK5pleJOgOFcmXxP_WHZRFhP9fRpLCuYpG6PpiuPdbT6lKT9eI8NWIGUzYMWPDSB4Aw8RWPFdriYowHPq0a4KqxG2QvZM375SS4gABYmuRk4ZBTLZO_8yn9d7wWCMJqWxO5WnHBV9zXshQKJ6RK71Q3Wd7AkdG-B1MQbxiMYzP33wq-GowwPZ5ADypGg7rBFjL1u8GsFcIZHTPqSlotxOs2T0Sqgz4_5K9w21YW8mWmEzYaVf03p9lSUceiJYkLkDkrqnvIHsTK5Cw70Zb9QUWqOmmVK0ZObxjIDW3xDxT9C1iuUSnLwUKmBo02jjUPOK8-YR8UHOZceh2llJVSVuE8iSBtI7XjbQI2NqG12AK08bjuse79zRNIsYXvHGyavyNDCBhHGGotQ8WFP4eiTAEtBD4n_7cuBXTxkAvXfBwoZakUkZbKJIt-m1_7FASzPLDnqNC_dlyRJFFKNE1nlZ8CSxmUzRP4Y8s7m97b0uhlzF9-UtyoBa6kgLSueImounAKtmo5P-J9Q2340H-qC-xiowI2kPcDfFC22GoUgEYnppku0m3ui-rKTNYTB5gg3IcvLuteHkNOAU6zSWNB9IEc67GSODWlREU-sZijqjfet4swYpmJnPH5OGAXthCrqIFZRxACiLmwkJsFuPRhvGcFMrTFAF0"},
{"dp", "CXTk7-y12QWtUMEJ_8fC8yiZhvva8Nu4r_RduzWp6Cs3SK4tMjnzoETuchHdC6sIfck7ruDjsRFXR0ebdjMtZscM-P2fx0N2XhkN63pNNEUP2mN427QfmXC7Xm9RBcBMFcAY3E-0rq5g8xevHNUeJ1FUtE7zdc95B-i68XNaEDwUKqZX8oTlyNis5Pa8tR8dmzZJc9elPZQcswXW7IGRnbwmQTnj-KtSdV9MptMcUezuepaoAJlUqJ1jzlA4JvsXWGVDQL3YDzlJJLAxShGiqOTCoe4FrpvrFRAx0TH1DMcE7WkaBAYlbpl9w0X-EP7r1-qKG35Ehd7qtnjEL_6obQ"},
{"dq", "6b73tkuFVXX4cDVBbJINp2nNlLcB7wGmrG5mx6AgM4W5_4j9DLQrJWFUm0DPXesyIEhUPOuXXT9xe3gYkglTKo8nL8ne4twKENtm4EPiu_4xrV3QB9vyuR8flXAEJGbK5kQGPm_9gKANwFnPjmpt5E81lpnyjd9-Hb_93LNpfL2pQLmiXiakbg5cGt6QLeaesvI94ACsOK33Z8NjBn5DkhJTdg5CCjfVkpz4p0Kdjk1oNgazB9fRKF2hqMHjFEflK6stMHrPI7uPTgXckvrCWH0SxudFtqujlW_Js6KPyFgzggRorj3gy6rfzLCgGcQcGvYUPxiImQFR9fbgJ3R_kQ"},
{"e", "AQAB"},
{"kty", "RSA"},
{"n", "0iSWi6QOkvGgjRhfuPpQ5enb9U5O2oRivU4r0r8r_A4lTc56qADyEy0_qer1W-1DhrFeDvL5bang8sk1F1ij9eKxGobpm9jre_9TqI1a-MVdFIaMoo9NOCoXgxKhGRpMhJspWJ_p8jOSlZpPKyCRo0mH_nP626NZPhJ_1dqIurn-8wOCr8Hd9MQW-Wsy4k7gWEOAgt7nb0H3JJR4M7vrwgn7abT7NOqe3q5GWKKvutCrV0jCv0_dTr44wFGrCXXj16xFYwFbzDrcvL1Vft6EXXWvXy9cEj72e7_qpSbGjGXMB64_QSyZoaQOR930MCiUwUzcepO6x9nxhBSdNU0d3CTKJU2B0a6n7gsqijal9oapVlYE0SFCnk98NuvhK3I3bXPLZpLd_o-t6M5QRLKRJDOBIXrT_X9qEvGrGuCca6STwY7BJcz3ombfrNzmvBYq9gOCw7jZzPF0cGFsKNo64dMA-IBcP92j_Fru3VjcW75HU3OKyhf0juDqUtN8_magR4YoSvXaDdxyngqQchxnIaYcrdAcQKBmqy0TjQuHfRCxL8UldyCUv1mRGbL15eKCZioraritOz4qU9-Z7OXdf6BWIX38bT7m0DSzOGMbcUtIrYb6UcMro7sNrkdFVE6VPlvObTVuAo9Pk26N7doxFrkAYkNA9Q9ou3d8FPW8gkU"},
{"p", "3S8t8G-gC0kfGlhTZfCI8kEdA_asjyKtMfsJpU_0G4L3CXPWYAlOOihyqTuruGDcE-RcGrr7PsUCzC_CZ0yGqlevmqQopXsfwRbXqoEROn8wX1JXRJy3PCW3MtxRl_dlmbCR2mj50pkXVzfu3vKRfXxwWArxRMpa8P6dtag_L4WcHXcxO3aeCP7hRNqxmO0QHCFRJHaBe3bMNZaP1mIip4n1KX4CD6ZiXtAguCquDOe7-1pSKK_l3XgW_xazXU3fD_Pobbx23q8V7MDGSELxy6_sJVYHXeKNhSRCo7PTTJGbLJaDqn-tHDloxbAWkM9J5eezEzh2yPK22n5TBwVWZw"},
{"q", "8zh8jvRPFVgT7M5aukRTWE_QpoBmpUMJ-YRzlkkQLOV9PcKpG3srtUNyWHdQBGZtGyDkakMhfr5HlHmxKCpKzZOpw0OYhSprz6NcOxfYuE45eHht7YOzejxirDRCi8yOeViXqmCvyPkAnpRnTgwEF4eqT5kGofBgPJo9LpnTpgYz0wJi4YEAkmY1CfX3AttGgGC03Tv6HryYIlDhDV6Qw3JAey2nM0DNzhDCVX-UoqAi3w6Bqy1EQJJ-YkdCMg30kZvi2Qur-OPSIEbtqg6Wwh6LxVEMk03Js8u14IJWu4VVAaAe5rJQnSpqSy3eB8GHGdv4BaUNC5VkNkXeobB-cw"},
{"qi", "rPWySGPdtn4wR2xHEQ62ZttUUlg6GED0qSbmJ7zrX-yAzME0Op3aCNLlNTe1mrpJwRjgbm2yjPSvnYj9VQiZY4ByRnx3UVIn_Lq_bd9xe0Qc0S3c2IXPtVYrtiss01QqAxy6Lp4rFiBS5yZSTk4iLe8_bRpGa9cirh8PC7OdqTMOov6z_eG_hod0sC_eyzaSy8JPMihAFdRLFnsgVBdujGfjGgRGqI0aSUCyG8zGO3Xz0c34nLg07uxBTck5sTwjOwrNiW2oU6wxJncQlxsQmxyiBhKCJ-M_JiJwJQG-SokIo_IbUEQrd7UYpNlTPa32ZOQ4eR2jcV-LQxAIJRX4tA"}
                    };

                    var c = "resource_indicators_demo_client";
                    var j = new Dictionary<string, object>
                    {
{"p", "_NrsQ3m2iqJJ-YhwQWPdhsJo3Z2-gUto2eFt-SZ_90ZF1t9zYwWMMhePBMf_KnXU1Uh_rHbiUCelqwfsUHeZdhzNf2FNdnH2Cmw3X39gYdP7pHGVEuB7lBaKnMDXGTsOMP5_iPNSR1lZrpq3YxYYnRSDO2-NsUQB1yzbFrPgnQE"},
{"kty", "RSA"},
{"q", "huLb5yFHjoKGFwWyMikKjwxoHkB6SMqzjsr0dSV_6S87I0buMT0RzMLhzHnS6o9E8HZIn8PytugI44iXY2I53TqAiuKQ9Pvk-1VagnGYW8J_76VTpWPBjl0uTjNKPSESj8y5MLUAeOhMLbbubsjdLSAEuVFTiLfeCr0U4yK0IjM"},
{"d", "G2OZEJuTv6rzuGgCIp1drC5w9tH-Oi3yS1yOGChdGylnZgHP-SD_3omyXI1w8OBJBvSwMMA7fPLiSs0VkChf5yA8RNXmT-4FB0EHUOY1e_mzqgGXDPUugK_GQoxYSpSKfDASzblg9Y-NFn3DgqrTUu8DRdUyDhW5tc4EtFHezBBS2pZqqdbNG7BPzpc-bTrUhtOu_38zbU6MyKWg_P0Ix7TxbXZL8O856P2ls2aY5K3LB1jQ7TrZUROtdajVn_HizPw-Xg78az5aLpNpqocsmwdAUe_7OAz2eNiBh-iDIBOXi0XFkch7D6WLP5ZQwICT9lTRXPnzvlyAl8z3pqnqAQ"},
{"e", "AQAB"},
{"use", "sig"},
{"kid", "36NpskF/5SWZBtJMn9W+VmvFeo0="},
{"qi", "Z54-5_Z9Nq5IV-l7_W0oZ0JjzK1VYwhKc1U-Y2f0hlrf2iFptc6RyX5t8Y22ss6BrWzJ-KRc8PvRYq4mSpjr1b2qYrOupUqvwfNO0TN2bpDEW3wBsuDh59ismQYUP4RhkAoN1193ga5RN5ExgU_lRtCB4KBUEVEk21u_W747Bbc"},
{"dp", "WY2DvgYec4002-7Jqy8eZzr1fv_-V7aIhRpFI8fR7Jbz5z0ulbSCSb90RMI8iiI_ZeaOaVuVncPc9e0RchJZxK5Azct9buS_ukNHfUgUr7EvX7dmj8_3lKRDJW9a_zGePhX0v2FGE8T-cUuUhcBuqTt5mxRQP6cRa88ULT853wE"},
{"alg", "PS256"},
{"dq", "dQzSNRzdAp7ji4Dm5L5WqlHi3DWpqBc2f_hA8JNtD0ZsNC2uL05GbwDCfvVMgT14Xo7WcMXSjsSGSiTS8mxfuEm3GE6J6f3Y8_1agI3g3-fFq4k1L_WEFm1n7HZ3utpDSEQohErdsQ4sZRM0jzCBNlJtpv8a1S5xYbI5OO15kZ8"},
{"n", "hTqqI3fOz6fmX9-rr4CffEmX78qW1Ttszi02QuRgqvr5sxGwhJG35svlvtKsTokIeqB3RwO_a042aavonUnvFGQCc9VFBvrK_hT85sbVWKl7VGg4q9lhObYQ9HpYGyYxdFDF30inVyEOFJz8asD5QAVJQ8kMDUvnq3qCRuwqS3_BZdTiAfLZQLuvoSLyGxh-uYUviPy-sghOvaUsn0zYZc3DOdBcyCPflPIc9TbjR9hYuGoFKsWlrg4HJqpptHXWAhr44o13MfDwTXw54Gs0djKGwSAcTqegPBso8VGjcGCqRm4J91SIvPAJ1Cg1-8p1Hf3jpg5xh5fjD0QVMU1pMw"}
                    };

                    //return new JwkTokenHandler(HelseIdUrl, clientId, jwtPrivateKey, ClientType.Machine);
                    return new JwkTokenHandler(HelseIdUrl, clientIdPers, jwtPrivateKeyPers, ClientType.Personal);
                    //return new JwkTokenHandler("https://helseid-sts.utvikling.nhn.no", c, j, ClientType.Personal);
                });

            var provider = serviceCollection.BuildServiceProvider();
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            using var httpClient = httpClientFactory.CreateClient("MeldeNo");

            // Fill out request data
            var requestData = new LegemiddelBivirkningRequest
            {
                EksternSaksId = "MYSYS-R195-1",
                Melder = new MelderPart
                {
                    Fødselsnummer = "13065906141",
                    Organisasjonsnummer = "974748924",//*/ "883974832",
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
