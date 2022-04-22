using OpenAPI;
using Shared.Configuration;

namespace Example.Configuration
{
    class Config
    {
        public static string HelseIdUrl => "https://helseid-sts.test.nhn.no";

        public static Uri ApiUri => SharedConfig.GetApiUri(Env.LOCAL);

        public static ClientType ClientType => ClientType.Person;

        public static string ClientId => "<Client id>";

        public static Dictionary<string, object> Jwk => new Dictionary<string, object>
        {
            // Jwk key parts ...
        };
    }
}
