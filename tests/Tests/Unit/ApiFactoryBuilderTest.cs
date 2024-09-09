using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System;
using TO_BE_REPLACED_PROJECT_NAME.Api;

namespace Finbourne.Sdk.Extensions.Tests.Unit
{
    [TestFixture]
    public class ApiFactoryBuilderTest
    {
        private string _secretsFile;
        [OneTimeSetUp]
        public void CreateDummySecretsFile()
        {
            _secretsFile = Path.GetTempFileName();
            var secrets = new Dictionary<string, object>
            {
                ["api"] = new Dictionary<string, string>()
                {
                    {"TO_BE_REPLACED_LOWERUrl", "https://sub-domain.lusid.com/api"},
                    {"tokenUrl", "https://sub-domain.lusid.com/oauth2/abcd123/v1/token"},
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
        public void WhenOptionsProvided_OptionsOverrideConfiguration()
        {
            // arrange
            var configurationOptions = new ConfigurationOptions
            {
                RateLimitRetries = 5,
                TimeoutMs = 33_000
            };
            Environment.SetEnvironmentVariable("FBN_ACCESS_TOKEN", "token");
            Environment.SetEnvironmentVariable("FBN_TO_BE_REPLACED_UPPER_URL", "http://localhost.lusid.com");
            Environment.SetEnvironmentVariable("FBN_TIMEOUT_MS", "66000");
            Environment.SetEnvironmentVariable("FBN_RATE_LIMIT_RETRIES", "6");
        
            // act
            var apiInstance = ApiFactoryBuilder.Build("", configurationOptions).Api<TEST_API>();
        
            // assert
            Assert.That(apiInstance.Configuration.TimeoutMs, Is.EqualTo(configurationOptions.TimeoutMs));
            Assert.That(apiInstance.Configuration.RateLimitRetries, Is.EqualTo(configurationOptions.RateLimitRetries));
        } 
    
        [Test]
        public void WhenNoOptionsProvided_ConfigurationUsed()
        {
            // arrange
            Environment.SetEnvironmentVariable("FBN_ACCESS_TOKEN", "token");
            Environment.SetEnvironmentVariable("FBN_TO_BE_REPLACED_UPPER_URL", "http://localhost.lusid.com");
            Environment.SetEnvironmentVariable("FBN_TIMEOUT_MS", "66000");
            Environment.SetEnvironmentVariable("FBN_RATE_LIMIT_RETRIES", "6");
        
            // act
            var apiInstance = ApiFactoryBuilder.Build("").Api<TEST_API>();
        
            // assert
            Assert.That(apiInstance.Configuration.TimeoutMs, Is.EqualTo(66_000));
            Assert.That(apiInstance.Configuration.RateLimitRetries, Is.EqualTo(6));
        } 
    }
}
