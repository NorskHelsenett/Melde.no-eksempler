using IdentityModel;
using IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Example.Toveisdialog.OpenAPIs
{
    public class HelseIdHelper
    {
        public static async Task<string> GetAccessToken(string stsUrl, string clientId, string jwkPrivateKey, string[] scopes, ClientType clientType, CancellationToken cancellationToken = default)
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
            if (clientType == ClientType.Machine)
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
                    },
                    ClientCredentialStyle = ClientCredentialStyle.PostBody,
                    Parameters = new Parameters(new Dictionary<string, string>
                    {
                        { "scope", string.Join(" ", scopes) }
                    })
                };
                var response = await c.RequestTokenAsync(tokenRequest, cancellationToken).ConfigureAwait(false);

                if (response.IsError)
                {
                    throw new Exception($"Error getting access token from HelseId. {response.Error}, ErrorDescription: {response.ErrorDescription}, stsUrl: {stsUrl}, clientId: {clientId}");
                }

                accessToken = response.AccessToken;
            }
            else if (clientType == ClientType.Person)
            {
                var tokenRequest = new PersonalTokenRequest()
                {
                    StsUrl = stsUrl,
                    ClientId = clientId,
                    ClientAssertion = new ClientAssertion
                    {
                        Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                        Value = BuildClientAssertion(disco, clientId, jwkPrivateKey)
                    },
                    Scopes = scopes
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
                new Claim(JwtClaimTypes.JwtId, Guid.NewGuid().ToString("N"))
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
