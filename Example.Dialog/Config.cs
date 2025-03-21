using OpenAPI;
using Shared.Configuration;
using System;
using System.Collections.Generic;

namespace Example.Configuration
{
    class Config
    {
        public static string HelseIdUrl => "https://helseid-sts.test.nhn.no";

        public static Uri ApiUri => SharedConfig.GetApiUri(Env.LOCAL);

        public static ClientType ClientType => ClientType.Person;

        // Currently DPoP tokens are supported for Machine-Machine only
        public static TokenType TokenType => TokenType.DPoPToken;

        public static string ClientId => "<Client id>";

        public static Dictionary<string, object> Jwk => new Dictionary<string, object>
        {
            // Jwk key parts ...
        };
    }
}
