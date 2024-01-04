using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Finbourne.Sdk.Extensions.Tests.Unit
{
    [TestFixture]
    public class ClientCredentialsFlowTokenProviderTest
    {
        [Test]
        public void Constructor_NonNull_Instance_Returned()
        {
            var secretsFile = Path.GetTempFileName();
            var secrets = new Dictionary<string, object>
            {
                ["api"] = new Dictionary<string, string>()
                {
                    {"lusidUrl", "https://sub-domain.lusid.com/api"},
                    {"tokenUrl", "https://sub-domain.okta.com/oauth2/abcd123/v1/token"},
                    {"clientId", "<clientId>"},
                    {"clientSecret", "<clientSecret>"},
                    {"applicationName", "<applicationName>"},
                    {"username", "<username>"},
                    {"password", "<password>"},
                }
            };
            var json = JsonSerializer.Serialize(secrets);
            File.WriteAllText(secretsFile, json);

            var tokenProvider = new ClientCredentialsFlowTokenProvider(ApiConfigurationBuilder.Build(secretsFile));
            Assert.IsNotNull(tokenProvider);
        }
    }
}
