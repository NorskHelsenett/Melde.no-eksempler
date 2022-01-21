using IdentityModel.Client;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAPIs
{
    public enum ClientType
    {
        Machine,
        Person
    }

    public class JwkTokenHandler : DelegatingHandler
    {
        private readonly string _stsUrl;
        private readonly string _clientId;
        private readonly string _jwtPrivateKey;
        private readonly string[] _scopes;
        private readonly ClientType _clientType;

        public JwkTokenHandler(string stsUrl, string clientId, Dictionary<string, object> jwtPrivateKey, string[] scopes, ClientType clientType)
        {
            _stsUrl = stsUrl;
            _clientId = clientId;
            _jwtPrivateKey = JsonConvert.SerializeObject(jwtPrivateKey);
            _scopes = scopes;
            _clientType = clientType;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_stsUrl))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var accessToken = await HelseIdTokenHelper.GetAccessToken(_stsUrl, _clientId, _jwtPrivateKey, _scopes, _clientType, cancellationToken);
            request.SetBearerToken(accessToken);

            // Proceed calling the inner handler, that will actually send the request
            // to our protected api
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
