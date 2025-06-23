using IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAPI
{
    public enum ClientType
    {
        Machine,
        Person
    }

    public enum TokenType
    {
        AccessToken,
        DPoPToken
    }

    public class JwkTokenHandler : DelegatingHandler
    {
        private readonly string _stsUrl;
        private readonly string _clientId;
        private readonly string _jwtPrivateKey;
        private readonly string[] _scopes;
        private readonly ClientType _clientType;
        private readonly TokenType _tokenType;

        public JwkTokenHandler(string stsUrl, string clientId, Dictionary<string, object> jwtPrivateKey, string[] scopes, ClientType clientType, TokenType tokenType, HttpClientHandler innerHandler) : base(innerHandler)
        {
            _stsUrl = stsUrl;
            _clientId = clientId;
            _jwtPrivateKey = JsonConvert.SerializeObject(jwtPrivateKey);
            _scopes = scopes;
            _clientType = clientType;
            _tokenType = tokenType;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var accessToken = await HelseIdTokenHelper.GetAccessToken(_stsUrl, _clientId, _jwtPrivateKey, _scopes, _clientType, _tokenType, cancellationToken);

            if(_tokenType == TokenType.AccessToken)
            {
                request.SetBearerToken(accessToken);
            }
            else if(_tokenType == TokenType.DPoPToken)
            {
                var dPopProof = new DPoPProofCreator(_jwtPrivateKey, SecurityAlgorithms.RsaSha256)
                    .CreateDPoPProof(request.RequestUri.AbsoluteUri, request.Method.ToString(), accessToken: accessToken);
                request.SetDPoPToken(accessToken, dPopProof);
            }

            // Proceed calling the inner handler, that will actually send the request
            // to our protected api
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
