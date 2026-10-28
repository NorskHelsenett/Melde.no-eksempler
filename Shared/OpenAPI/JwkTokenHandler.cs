using HelseId.Library;
using HelseId.Library.ClientCredentials;
using HelseId.Library.ClientCredentials.Interfaces;
using HelseId.Library.Configuration;
using HelseId.Library.ExtensionMethods;
using HelseId.Library.Interfaces.JwtTokens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAPI
{
    public class JwkTokenHandler : DelegatingHandler
    {
        private static IServiceCollection _services;
        private static IServiceProvider _serviceProvider;

        public JwkTokenHandler(string stsUrl, string clientId, string jwtPrivateKey, string[] scopes, HttpClientHandler innerHandler) : base(innerHandler)
        {
            if (_serviceProvider is null)
            {
                _services = CreateServices(stsUrl, clientId, jwtPrivateKey, scopes);
                _serviceProvider = _services.BuildServiceProvider();
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var ccFlow = _serviceProvider.GetRequiredService<IHelseIdClientCredentialsFlow>();
            var dPoPProofCreator = _serviceProvider.GetRequiredService<IDPoPProofCreatorForApiRequests>();
            var tokenResponse = await ccFlow.GetTokenResponseAsync();
            if (tokenResponse.IsSuccessful(out var accessTokenResponse))
            {
                var dPoPProof = await dPoPProofCreator.CreateDPoPProofForApiRequest(
                    request.Method,
                    request.RequestUri?.GetLeftPart(UriPartial.Path) ?? "",
                    accessTokenResponse);
                request.SetDPoPTokenAndProof(accessTokenResponse, dPoPProof);
                return await base.SendAsync(request, cancellationToken);
            }
            else
            {
                var logger = _serviceProvider.GetRequiredService<ILogger<JwkTokenHandler>>();
                var errorResponse = tokenResponse.AsError();
                logger.LogError("HelseId exception: {ErrorDescription}", errorResponse.ErrorDescription);
                throw new Exception($"HelseId exception: {errorResponse.ErrorDescription}");
            }
        }

        IServiceCollection CreateServices(string stsUrl, string clientId, string jwtPrivateKey, string[] scopes)
        {
            var services = new ServiceCollection();
            var config = new HelseIdConfiguration
            {
                IssuerUri = stsUrl,
                ClientId = clientId,
                Scope = string.Join(",", scopes),
            };

            services.AddHelseIdClientCredentials(config)
                .AddJwkForClientAuthentication(jwtPrivateKey);

            return services;
        }
    }
}
