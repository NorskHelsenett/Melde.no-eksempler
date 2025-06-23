using IdentityModel;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityModel.Client;

namespace OpenAPI
{
    public class HelseIdTokenHelper
    {
        public static async Task<string> GetAccessToken(string stsUrl, string clientId, string jwkPrivateKey, string[] scopes, ClientType clientType, TokenType tokenType, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(stsUrl)) throw new ArgumentException(nameof(stsUrl));
            if (string.IsNullOrEmpty(clientId)) throw new ArgumentException(nameof(clientId));
            if (string.IsNullOrEmpty(jwkPrivateKey)) throw new ArgumentException(nameof(jwkPrivateKey));

            var c = new HttpClient();
            var disco = await c.GetDiscoveryDocumentAsync(stsUrl);
            if (disco.IsError)
            {
                throw new Exception($"Error getting discovery document from HelseId: {disco.Error}, stsUrl: {stsUrl}");
            }

            var useDpop = tokenType == TokenType.DPoPToken;
            string nonce = null;
            TokenResponse response = null;
            string accessToken = "";

            if (clientType == ClientType.Machine)
            {
                for (int i = 0; i < (useDpop ? 2 : 1); i++)
                {
                    var tokenRequest = new ClientCredentialsTokenRequest
                    {
                        Address = disco.TokenEndpoint,
                        ClientId = clientId,
                        GrantType = OidcConstants.GrantTypes.ClientCredentials,
                        ClientCredentialStyle = ClientCredentialStyle.PostBody,
                        ClientAssertion = new ClientAssertion
                        {
                            Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                            Value = BuildClientAssertion(disco, clientId, jwkPrivateKey)
                        },
                        Scope = string.Join(' ', scopes),

                        // Dpop
                        DPoPProofToken = useDpop ? new DPoPProofCreator(jwkPrivateKey, SecurityAlgorithms.RsaSha256)
                            .CreateDPoPProof(disco.TokenEndpoint!, "POST", dPoPNonce: nonce) : null
                    };
                    response = await c.RequestTokenAsync(tokenRequest);

                    if (response.IsError && response.Error == "use_dpop_nonce" && !string.IsNullOrEmpty(response.DPoPNonce))
                    {
                        nonce = response.DPoPNonce;
                    }
                    else
                        break;
                }

                if (response!.IsError)
                {
                    throw new Exception($"Error getting access token from HelseId. {response.Error}, ErrorDescription: {response.ErrorDescription}, stsUrl: {stsUrl}, clientId: {clientId}");
                }

                accessToken =  response?.AccessToken;
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
                    Scopes = scopes,

                    // Dpop
                    DPoPProofToken = useDpop ? new DPoPProofCreator(jwkPrivateKey, SecurityAlgorithms.RsaSha256)
                            .CreateDPoPProof(disco.TokenEndpoint!, "POST", dPoPNonce: nonce) : null
                };
                accessToken = await tokenRequest.RequestTokenAsync().ConfigureAwait(false);
            }

            return accessToken;
        }

        public static string BuildClientAssertion(DiscoveryDocumentResponse disco, string clientId, string jwkPrivateKey)
        {
            var claims = new Dictionary<string, object>
            {
                { JwtRegisteredClaimNames.Sub, clientId },
                { JwtRegisteredClaimNames.Iat, DateTimeOffset.Now.ToUnixTimeSeconds().ToString() },
                { JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N") }
            };

            var credentials = new SecurityTokenDescriptor
            {
                Issuer = clientId,
                Audience = disco.Issuer,
                TokenType = "client-authentication+jwt",
                Claims = claims,
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddSeconds(10),
                SigningCredentials = GetClientAssertionSigningCredentials(jwkPrivateKey)
            };

            var tokenHandler = new JsonWebTokenHandler();
            return tokenHandler.CreateToken(credentials);
        }

        private static SigningCredentials GetClientAssertionSigningCredentials(string jwkPrivateKey)
        {
            var securityKey = new JsonWebKey(jwkPrivateKey);
            return new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
        }
    }
}
