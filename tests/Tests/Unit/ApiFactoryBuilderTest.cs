using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System;
using Lusid.Sdk.Api;

namespace Finbourne.Sdk.Extensions.Tests.Unit
{
    [TestFixture]
    public class ApiFactoryBuilderTest
    {
        private readonly string AppUrlName = $"{Environment.GetEnvironmentVariable("FBN_API_TEST_APP_NAME").ToLower()}Url";
        private string _secretsFile;
        [OneTimeSetUp]
        public void CreateDummySecretsFile()
        {
            _secretsFile = Path.GetTempFileName();
            var secrets = new Dictionary<string, object>
            {
                ["api"] = new Dictionary<string, string>()
                {
                    {AppUrlName, "https://sub-domain.lusid.com/api"},
                    {"tokenUrl", "https://sub-domain.okta.com/oauth2/abcd123/v1/token"},
                    {"clientId", "<clientId>"},
                    {"clientSecret", "<clientSecret>"},
                    {"applicationName", "<applicationName>"},
                    {"username", "<username>"},
                    {"password", "<password>"},
                }
            };
            var json = JsonSerializer.Serialize(secrets);
            File.WriteAllText(_secretsFile, json);
        }

        [Test]
        public void Build_From_Secrets_Returns_NonNull_ApiFactory()
        {
            var apiConfig = ApiConfigurationBuilder.Build(_secretsFile);
            var apiFactory = new ApiFactory(apiConfig);
            Assert.IsNotNull(apiFactory);
        }

        //Test requires [assembly: InternalsVisibleTo("Finbourne.Sdk.Extensions.Tests")] in the SDK project.
        [Test]
        public void Build_From_Configuration_Returns_NonNull_ApiFactory()
        {
            var config = new TokenProviderConfiguration(new ClientCredentialsFlowTokenProvider(ApiConfigurationBuilder.Build(_secretsFile)))
            {
                BasePath = "base path"
            };
            var apiFactory = new ApiFactory(config);
            Assert.IsNotNull(apiFactory);
        }

        [Test]
        public void Custom_Builder_Parameters()
        {
            var config = new TokenProviderConfiguration(new ClientCredentialsFlowTokenProvider(ApiConfigurationBuilder.Build(_secretsFile)))
            {
                BasePath = "base path"
            };
            string key = "header_key";
            var testHeaders = new Dictionary<string, string>() {{key, "testApp"}};
            int timeOut = 50;
            var apiFactory = new ApiFactory(config, testHeaders, timeOut);

            Assert.IsNotNull(apiFactory);
            Assert.That(apiFactory.Api<ApplicationMetadataApi>().Configuration.DefaultHeaders, Does.ContainKey(key));
        }

        [Test]
        public void Null_Custom_Builder_Parameters()
        {
            var config = new TokenProviderConfiguration(new ClientCredentialsFlowTokenProvider(ApiConfigurationBuilder.Build(_secretsFile)))
            {
                BasePath = "base path"
            };
            string key = "header_key";
            var apiFactory = new ApiFactory(config, null, null);

            Assert.IsNotNull(apiFactory);
            Assert.That(apiFactory.Api<ApplicationMetadataApi>().Configuration.DefaultHeaders, !Does.ContainKey(key));
        }
    }
}
