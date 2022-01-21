﻿using IdentityModel;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OpenAPIs
{
    /*
     * This sample application shows how Resource Indicators 
     * (https://datatracker.ietf.org/doc/html/rfc8707)
     * to download multiple Access Tokens without performing multiple
     * calls to the Authorize endpoint.
     * This is important when calling national health APIs since most
     * of these require Access Tokens where they are the only audience.
     * 
     * This sample app has access to two APIs: udelt:resource_indicator_api_1
     * and udelt:resource_indicator_api_2. The app requests scopes from 
     * both APIs but the first Token-call only requests the first resource.
     * The second Token-call (using a Refresh Token) requests the second
     * resource.
     */
    class PersonalTokenRequest
    {
        const string Localhost = "http://localhost:8089";
        const string RedirectUrl = "/callback";
        const string StartPage = "/start";

        public PersonalTokenRequest()
        {

        }

        public string StsUrl { get; set; }
        public string ClientId { get; set; }
        public ClientAssertion ClientAssertion { get; set; }

        public async Task<string> RequestTokenAsync()
        {
            try
            {
                //var httpClient = new HttpClient();
                //var disco = await httpClient.GetDiscoveryDocumentAsync(StsUrl);
                //if (disco.IsError)
                //{
                //    throw new Exception(disco.Error);
                //}

                // 1. Logging in the user
                // ///////////////////////
                // Perfom user login, uses the /authorize endpoint in HelseID
                // Use the Resource-parameter to indicate which API-s you want tokens for
                // Use the Scope-parameter to indicate which scopes you want for these API-s

                //var clientAssertionPayload = GetClientAssertionPayload(disco, ClientId, "JWK");
                var oidcClient = new OidcClient(new OidcClientOptions
                {                    
                    Authority = StsUrl,
                    LoadProfile = false,
                    RedirectUri = $"{Localhost}{RedirectUrl}",
                    Scope = "openid profile offline_access",
                    ClientId = ClientId,
                    //Resource = new List<string> { "udelt:resource_indicator_api_1" },
                    ClientAssertion = ClientAssertion,                   

                    Policy = new Policy { ValidateTokenIssuerName = true },                    
                });

                var state = await oidcClient.PrepareLoginAsync();
                var response = await RunLocalWebBrowserUntilCallback(Localhost, RedirectUrl, StartPage, state);



                // 2. Retrieving an access token for API 1, and a refresh token
                ///////////////////////////////////////////////////////////////////////
                // User login has finished, now we want to request tokens from the /token endpoint
                // We add a Resource parameter indication that we want scopes for API 1
                var parameters = new Parameters
                {
                    //{ "resource", firstResource }
                };
                var loginResult = await oidcClient.ProcessResponseAsync(response, state, parameters);

                if (loginResult.IsError)
                {
                    throw new Exception(loginResult.Error);
                }
                var accessToken1 = loginResult.AccessToken;
                var refreshToken = loginResult.RefreshToken;


                Console.WriteLine("Access Token: " + accessToken1);
                Console.WriteLine("Refresh Token: " + refreshToken);
                Console.WriteLine();

                return accessToken1;

                //// 3. Using the refresh token to get an access token for API 2
                ////////////////////////////////////////////////////////////////
                //// Now we want a second access token to be used for API 2
                //// Again we use the /token-endpoint, but now we use the refresh token
                //// The Resource parameter indicates that we want a token for API 2.
                //var refreshTokenRequest = new RefreshTokenRequest
                //{
                //    Address = disco.TokenEndpoint,
                //    ClientId = ClientId,
                //    RefreshToken = refreshToken,
                //    Resource = new List<string> { secondResource },
                //    ClientAssertion = GetClientAssertionPayload(disco, ClientId, "JWK")
                //};

                //var refreshTokenResult = await httpClient.RequestRefreshTokenAsync(refreshTokenRequest);

                //if (refreshTokenResult.IsError)
                //{
                //    throw new Exception(refreshTokenResult.Error);
                //}

                //Console.WriteLine("Second request, resource: " + secondResource);
                //Console.WriteLine("Access Token: " + refreshTokenResult.AccessToken);
                //Console.WriteLine("Refresh Token: " + refreshTokenResult.RefreshToken);
                //Console.WriteLine();

            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error:");
                Console.Error.WriteLine(e.ToString());
            }
            return null;
        }

        private static ClientAssertion GetClientAssertionPayload(DiscoveryDocumentResponse disco, string clientId, string jwk)
        {
            var clientAssertionString = HelseIdTokenHelper.BuildClientAssertion(disco, clientId, jwk);
            return new ClientAssertion
            {
                Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                Value = clientAssertionString
            };

        }

        //private static string BuildClientAssertion(string clientId, DiscoveryDocumentResponse disco)
        //{
        //    var claims = new List<Claim>
        //    {
        //        new Claim(JwtClaimTypes.Subject, clientId),
        //        new Claim(JwtClaimTypes.IssuedAt, DateTimeOffset.Now.ToUnixTimeSeconds().ToString()),
        //        new Claim(JwtClaimTypes.JwtId, Guid.NewGuid().ToString("N")),
        //    };

        //    var credentials = new JwtSecurityToken(clientId, disco.TokenEndpoint, claims, DateTime.UtcNow, DateTime.UtcNow.AddSeconds(60), GetClientAssertionSigningCredentials());

        //    var tokenHandler = new JwtSecurityTokenHandler();
        //    return tokenHandler.WriteToken(credentials);
        //}

        //private static SigningCredentials GetClientAssertionSigningCredentials()
        //{
        //    var jwk = File.ReadAllText("jwk.json");
        //    var securityKey = new JsonWebKey(jwk);
        //    return new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
        //}

        private static async Task<string> RunLocalWebBrowserUntilCallback(string localhost, string redirectUrl, string startPage, AuthorizeState state)
        {
            // Build a HTML form that does a POST of the data from the url
            // This is a workaround since the url may be too long to pass to the browser directly
            var startPageHtml = UrlToHtmlForm.Parse(state.StartUrl);

            // Setup a temporary http server that listens to the given redirect uri and to 
            // the given start page. At the start page we can publish the html that we
            // generated from the StartUrl and at the redirect uri we can retrieve the 
            // authorization code and return it to the application
            var listener = new ContainedHttpServer(localhost, redirectUrl,
                new Dictionary<string, Action<HttpContext>> {
                    { startPage, async ctx => await ctx.Response.WriteAsync(startPageHtml)}
                });

            RunBrowser(localhost + startPage);

            return await listener.WaitForCallbackAsync();
        }

        private static void RunBrowser(string url)
        {
            // Thanks Brock! https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
    }
}
