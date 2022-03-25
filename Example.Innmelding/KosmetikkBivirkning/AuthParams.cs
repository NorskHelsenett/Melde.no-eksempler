using OpenAPI;
using System.Collections.Generic;

namespace Example.Auth
{
    class AuthParams
    {
        public static ClientType ClientType => ClientType.Person;

        public static string ClientId => "<Client id>";

        public static Dictionary<string, object> Jwk => new Dictionary<string, object>
        {
            // Jwk key parts ...
        };
    }
}
