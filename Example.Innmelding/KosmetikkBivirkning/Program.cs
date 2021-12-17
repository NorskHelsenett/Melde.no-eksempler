﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using MeldeApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;


namespace Example.Kosmetikk
{
    class Program
    {
        // Points to Melde.no API base
        //private static readonly Uri ApiBaseAddress = new ("https://localhost:44342/");
        private static readonly Uri ApiBaseAddress = new("https://api.test.melde.no/");

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
                    var clientId = "<client id>";
                    var jwtPrivateKey = new Dictionary<string, object>
                    {
                        // ... key parts
                    };

                    return new JwkTokenHandler(HelseIdUrl, clientId, jwtPrivateKey);
                });

            var provider = serviceCollection.BuildServiceProvider();
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            using var httpClient = httpClientFactory.CreateClient("MeldeNo");

            //// Fill out request data
            var requestData = new KosmetikkRequest
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
                        KlokkeslettForHendelsen = new Klokkeslett
                        {
                            Timer = 10,
                            Minutter = 12
                        }
                    }
                },
                Pasient = new Pasienten
                {
                    Kjonn = PasientensKjonn.Mann,
                    Alder = 40
                },
                Bivirkning = new KosmetikkBivirkningPart
                {
                    BivirkningHvorPaKroppen = new List<HvorPaKroppen>
                    {
                        HvorPaKroppen.Ansikt,
                        HvorPaKroppen.Mage,
                    },
                    Reaksjon = new List<Reaksjon>
                    {
                        Reaksjon.EksemUtslett,
                        Reaksjon.Hevelse
                    },
                    FolgerAvBivirkning = FolgerAvBivirkning.Sykehusopphold,
                    Reaksjonstid = Reaksjonstid.Innen30Min,
                },
                RelevanteOpplysninger = new RelevanteOpplysningerPart
                {
                    BivirkningerVedTidligereBruk = BivirkningVedTidligereBruk.Delvis
                },
                Produkter = new List<KosmetikkProduktPart>
                {
                    new KosmetikkProduktPart
                    {
                        Produktinformasjon = new ProduktinformasjonPart
                        {
                            ProduktNavn = "Lano",
                            ProduktType = Produkttype.Sape,
                            Salgskanal = Salgskanal.Matvarebutikk
                        },
                        BrukAvProduktet = new BrukAvProduktetPart
                        {
                            ProduktetBruktHvorPaKroppen = new List<HvorPaKroppen>
                            {
                                HvorPaKroppen.Ansikt,
                                HvorPaKroppen.Mage
                            },
                            BeskrivelseAvBruk = "Vårrengjøring"
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

        public class JwkTokenHandler : DelegatingHandler
        {
            private readonly string _stsUrl;
            private readonly string _clientId;
            private readonly string _jwtPrivateKey;

            public JwkTokenHandler(string stsUrl, string clientId, Dictionary<string, object> jwtPrivateKey)
            {
                _stsUrl = stsUrl;
                _clientId = clientId;
                _jwtPrivateKey = JsonConvert.SerializeObject(jwtPrivateKey);
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                if (string.IsNullOrWhiteSpace(_stsUrl))
                {
                    return await base.SendAsync(request, cancellationToken);
                }

                var accessToken = await HelseIdTokenHelper.GetAccessToken(_stsUrl, _clientId, _jwtPrivateKey, cancellationToken);
                request.SetBearerToken(accessToken);

                // Proceed calling the inner handler, that will actually send the request
                // to our protected api
                return await base.SendAsync(request, cancellationToken);
            }
        }

        /// <summary>
        /// Convenience class for communicating with HelseId,
        /// e.g. obtaining an OAuth access token.
        /// <remarks>
        /// The contents are mostly ripped from the HelseId Github demo HelseId.Demo.ClientCredentials.Jwk
        /// </remarks>
        /// </summary>
        public class HelseIdTokenHelper
        {
            /// <summary>
            /// Gets an access token.
            /// </summary>
            /// <param name="stsUrl"></param>
            /// <param name="clientId"></param>
            /// <param name="jwkPrivateKey">A string containing JSON</param>
            /// <param name="cancellationToken"></param>
            /// <example>
            /// <code>
            /// var jwkPrivateKey = @"{
            ///     ""kty"":""EC"",
            ///     ""crv"":""P-256"",
            ///     ""x"":""f83OJ3D2xF1Bg8vub9tLe1gHMzV76e8Tus9uPHvRVEU"",
            ///     ""y"":""x_FEzRu9m36HLN_tue659LNpXW6pCyStikYjKIWI5a0"",
            ///     ""kid"":""Public key used in JWS spec Appendix A.3 example""
            /// }";
            /// var accessToken = await HelseIdTokenHelper.GetAccessToken(
            ///     "https://helseid-sts.test.nhn.no",
            ///     "4323423-234234",
            ///     jwkPrivateKey);
            /// </code>
            /// </example>
            /// <returns></returns>
            public static async Task<string> GetAccessToken(string stsUrl, string clientId, string jwkPrivateKey, CancellationToken cancellationToken = default)
            {
                if (string.IsNullOrEmpty(stsUrl)) throw new ArgumentException(nameof(stsUrl));
                if (string.IsNullOrEmpty(clientId)) throw new ArgumentException(nameof(clientId));
                if (string.IsNullOrEmpty(jwkPrivateKey)) throw new ArgumentException(nameof(jwkPrivateKey));

                // TODO: HttpClientFactory ?
                var c = new HttpClient();
                var disco = await c.GetDiscoveryDocumentAsync(stsUrl, cancellationToken);
                if (disco.IsError)
                {
                    throw new Exception($"Error getting discovery document from HelseId: {disco.Error}, stsUrl: {stsUrl}");
                }

                var tokenRequest = new TokenRequest
                {
                    Address = disco.TokenEndpoint,
                    ClientId = clientId,
                    GrantType = OidcConstants.GrantTypes.ClientCredentials,
                    ClientAssertion = new ClientAssertion
                    {
                        Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                        Value = BuildClientAssertion(disco, clientId, jwkPrivateKey)
                    }
                };
                var response = await c.RequestTokenAsync(tokenRequest, cancellationToken);

                if (response.IsError)
                {
                    throw new Exception($"Error getting access token from HelseId. {response.Error}, ErrorDescription: {response.ErrorDescription}, stsUrl: {stsUrl}, clientId: {clientId}");
                }

                return response.AccessToken;
            }

            private static string BuildClientAssertion(DiscoveryDocumentResponse disco, string clientId, string jwkPrivateKey)
            {
                var claims = new List<Claim>
            {
                new Claim(JwtClaimTypes.Subject, clientId),
                new Claim(JwtClaimTypes.IssuedAt, DateTimeOffset.Now.ToUnixTimeSeconds().ToString()),
                new Claim(JwtClaimTypes.JwtId, Guid.NewGuid().ToString("N")),
            };

                var credentials = new JwtSecurityToken(
                    clientId,
                    disco.TokenEndpoint,
                    claims,
                    DateTime.UtcNow,
                    DateTime.UtcNow.AddSeconds(60),
                    GetClientAssertionSigningCredentials(jwkPrivateKey));

                var tokenHandler = new JwtSecurityTokenHandler();
                return tokenHandler.WriteToken(credentials);
            }

            private static SigningCredentials GetClientAssertionSigningCredentials(string jwkPrivateKey)
            {
                var securityKey = new JsonWebKey(jwkPrivateKey);
                return new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
            }
        }
    }
}