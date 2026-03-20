using System;

namespace Shared.Configuration
{
    public enum Env { LOCAL, DEV, TEST }

    public class SharedConfig
    {
        public static Uri GetApiUri(Env e)
        {
            return e switch
            {
                Env.DEV => new("https://api.melde.dev.sky.nhn.no/"),
                Env.TEST => new("https://api.melde.test.nhn.no/"),
                _ => new("https://localhost:44342/"),
            };
        }
    }
}
