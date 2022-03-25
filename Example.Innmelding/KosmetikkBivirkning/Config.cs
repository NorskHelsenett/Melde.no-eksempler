using OpenAPI;
using Shared.Configuration;
using System;
using System.Collections.Generic;

namespace Example.Configuration
{
    class Config
    {
        public static Uri ApiUri => SharedConfig.GetApiUri(Env.LOCAL);

        public static ClientType ClientType => ClientType.Person;

        public static string ClientId => "<Client id>";

        public static Dictionary<string, object> Jwk => new Dictionary<string, object>
        {
            // Jwk key parts ...
        };
    }
}
