using IdentityModel;
using IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAPIs
{
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
        public static async Task<string> GetAccessToken(string stsUrl, string clientId, string jwkPrivateKey, ClientType clientType, CancellationToken cancellationToken = default)
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

            var accessToken = null as string;
            if(clientType == ClientType.Machine)
            {
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
                var response = await c.RequestTokenAsync(tokenRequest, cancellationToken).ConfigureAwait(false);

                if (response.IsError)
                {
                    throw new Exception($"Error getting access token from HelseId. {response.Error}, ErrorDescription: {response.ErrorDescription}, stsUrl: {stsUrl}, clientId: {clientId}");
                }

                accessToken = response.AccessToken;
            }
            else
            {
                //await Program.Main();

                var tokenRequest = new PersonalTokenRequest()
                {
                    StsUrl = stsUrl,
                    ClientId = clientId,
                    ClientAssertion = new ClientAssertion
                    {
                        Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                        Value = BuildClientAssertion(disco, clientId, jwkPrivateKey)
                    }
                };
                accessToken = await tokenRequest.RequestTokenAsync().ConfigureAwait(false);
            }
            return accessToken;
        }

        public static string BuildClientAssertion(DiscoveryDocumentResponse disco, string clientId, string jwkPrivateKey)
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
