using System;

namespace Shared.Configuration
{
    public enum Env { LOCAL, TEST, QA }

    public class SharedConfig
    {
        public static Uri GetApiUri(Env e)
        {
            return e switch
            {
                Env.TEST => new("https://api.test.melde.no/"),
                Env.QA => new("https://api.qa.melde.no/"),
                _ => new("https://localhost:44342/"),
            };
        }
    }
}
