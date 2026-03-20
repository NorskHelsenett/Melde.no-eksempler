using Shared.Configuration;
using System;

namespace Example.Configuration
{
    class Config
    {
        public static string HelseIdUrl => "https://helseid-sts.test.nhn.no";

        public static Uri ApiUri => SharedConfig.GetApiUri(Env.LOCAL);

        public static string ClientId => "<Client id>";

        public static string Jwk => "";
    }
}
