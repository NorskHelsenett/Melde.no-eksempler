using IdentityModel;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient.DPoP;
using Duende.IdentityModel.OidcClient.Browser;
using System.Threading;

namespace OpenAPI
{
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
        public string[] Scopes { get; set; }
        public string DPoPProofToken { get; set; }
        public ClientAssertion ClientAssertion { get; set; }

        public async Task<string> RequestTokenAsync()
        {
            try
            {
                // 1. Logging in the user
                // ///////////////////////
                // Perfom user login, uses the /authorize endpoint in HelseID
                // Use the Scope-parameter to indicate which scopes you want for these API-s

                //var clientAssertionPayload = GetClientAssertionPayload(disco, ClientId, "JWK");
                var oidcClient = new OidcClient(new OidcClientOptions
                {                    
                    Authority = StsUrl,
                    LoadProfile = false,
                    RedirectUri = $"{Localhost}{RedirectUrl}",
                    Scope = "openid profile offline_access " + string.Join(" ", Scopes),
                    ClientId = ClientId,
                    ClientAssertion = ClientAssertion,

                    //Browser = new SystemBrowser()

                    Policy = new Policy { ValidateTokenIssuerName = true }
                });

                //var result = await oidcClient.LoginAsync();

                Console.WriteLine();

                var state = await oidcClient.PrepareLoginAsync();
                var response = await RunLocalWebBrowserUntilCallback(Localhost, RedirectUrl, StartPage, state);

                // 2. Retrieving an access token for API 1, and a refresh token
                ///////////////////////////////////////////////////////////////////////
                // User login has finished, now we want to request tokens from the /token endpoint
                // We add a Resource parameter indication that we want scopes for API 1
                var loginResult = await oidcClient.ProcessResponseAsync(response, state);

                if (loginResult.IsError)
                {
                    throw new Exception(loginResult.Error);
                }
                var accessToken1 = loginResult.AccessToken;
                var refreshToken = loginResult.RefreshToken;

                return accessToken1;
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

        private static async Task<string> RunLocalWebBrowserUntilCallback(string localhost, string redirectUrl, string startPage, AuthorizeState state)
        {
            // Build a HTML form that does a POST of the data from the url
            // This is a workaround since the url may be too long to pass to the browser directly
            var startPageHtml = UrlToHtmlForm.Parse(state.StartUrl);

            // Setup a temporary http server that listens to the given redirect uri and to 
            // the given start page. At the start page we can publish the html that we
            // generated from the StartUrl and at the redirect uri we can retrieve the 
            // authorization code and return it to the application
            using (var listener = new ContainedHttpServer(localhost, redirectUrl,
                new Dictionary<string, Action<HttpContext>> {
                    { startPage, async ctx => await ctx.Response.WriteAsync(startPageHtml)}
                }))
            {
                RunBrowser(localhost + startPage);

                return await listener.WaitForCallbackAsync();
            }
        }

        private static void RunBrowser(string url)
        {
            // Thanks Brock! https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
    }

#if false
    class Briowser : IBrowser
    {
        const string Localhost = "http://localhost:8089";
        const string RedirectUrl = "/callback";
        const string StartPage = "/start";

        public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            var result = RunLocalWebBrowserUntilCallback(Localhost, RedirectUrl, StartPage, state);

        }

        private static async Task<string> RunLocalWebBrowserUntilCallback(string localhost, string redirectUrl, string startPage, AuthorizeState state)
        {
            // Build a HTML form that does a POST of the data from the url
            // This is a workaround since the url may be too long to pass to the browser directly
            var startPageHtml = UrlToHtmlForm.Parse(state.StartUrl);

            // Setup a temporary http server that listens to the given redirect uri and to 
            // the given start page. At the start page we can publish the html that we
            // generated from the StartUrl and at the redirect uri we can retrieve the 
            // authorization code and return it to the application
            using (var listener = new ContainedHttpServer(localhost, redirectUrl,
                new Dictionary<string, Action<HttpContext>> {
                    { startPage, async ctx => await ctx.Response.WriteAsync(startPageHtml)}
                }))
            {
                RunBrowser(localhost + startPage);

                return await listener.WaitForCallbackAsync();
            }
        }

        private static void RunBrowser(string url)
        {
            // Thanks Brock! https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
    }
#endif
}
