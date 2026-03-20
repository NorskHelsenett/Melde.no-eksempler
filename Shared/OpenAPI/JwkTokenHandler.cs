using IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAPI
{
    public class JwkTokenHandler : DelegatingHandler
    {
        private readonly string _stsUrl;
        private readonly string _clientId;
        private readonly string _jwtPrivateKey;
        private readonly string[] _scopes;

        public JwkTokenHandler(string stsUrl, string clientId, string jwtPrivateKey, string[] scopes, HttpClientHandler innerHandler) : base(innerHandler)
        {
            _stsUrl = stsUrl;
            _clientId = clientId;
            _jwtPrivateKey = jwtPrivateKey;
            _scopes = scopes;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var accessToken = await HelseIdTokenHelper.GetAccessToken(_stsUrl, _clientId, _jwtPrivateKey, _scopes, cancellationToken);
            var dPopProof = new DPoPProofCreator(_jwtPrivateKey, SecurityAlgorithms.RsaSha256)
                .CreateDPoPProof(request.RequestUri.GetLeftPart(UriPartial.Path), request.Method.ToString(), accessToken: accessToken);
            request.SetDPoPToken(accessToken, dPopProof);

            // Proceed calling the inner handler, that will actually send the request
            // to our protected api
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
