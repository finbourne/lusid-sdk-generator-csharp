using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using static System.Environment;

namespace Finbourne.Sdk.Extensions.Tests.Unit
{
    [TestFixture]
    public class ApiConfigurationBuilderTest
    {
        private string _secretsFile;
        private const string TestAppName = "myapp";
        private const string TestBaseUrl = "https://some-domain.lusid.com";
        private const string TestTokenUrl = "https://some-domain.identity.lusid.com/oauth2/abc/v1/token";
        private const string TestUsername = "username";
        private const string TestPassword = "password";
        private const string TestClientId = "client-id";
        private const string TestClientSecret = "client-secret";
        private const string TestAccessToken = "access-token";
        private const int TestTimeoutMs = 2000;
        private const int TestRateLimitRetries = 3;

        [SetUp]
        public void SetUp()
        {
            ClearEnvironmentVariables();
            _secretsFile = Path.GetTempFileName();
        }

        [TearDown]
        public void TearDown()
        {
            ClearEnvironmentVariables();
            File.Delete(_secretsFile);
        }

        [Test]
        public void WhenFileExists_ConfigurationTakenFromFile()
        {
            // arrange
            PopulateDummySecretsFile(new Dictionary<string, object>
            {
                {"applicationName", TestAppName},
                {"tokenUrl", TestTokenUrl},
                {"username", TestUsername},
                {"password", TestPassword},
                {"clientId", TestClientId},
                {"clientSecret", TestClientSecret},
                {"TO_BE_REPLACED_LOWERUrl", TestBaseUrl},
                {"accessToken", TestAccessToken},
                {"timeoutMs", TestTimeoutMs},
                {"rateLimitRetries", TestRateLimitRetries}
            });
            
            // act
            var config = ApiConfigurationBuilder.Build(_secretsFile);

            // assert
            AssertExpectedConfigSet(config);
        }

        [Test]
        public void WhenOptionsAndFile_OptionsOverrideFileConfig()
        {
            // arrange
            PopulateDummySecretsFile(new Dictionary<string, object>
            {
                {"TO_BE_REPLACED_LOWERUrl", TestBaseUrl},
                {"accessToken", TestAccessToken},
                {"timeoutMs", TestTimeoutMs},
                {"rateLimitRetries", TestRateLimitRetries}
            });
            
            // act
            var opts = new ConfigurationOptions
            {
                TimeoutMs = 3000,
                RateLimitRetries = 4
            };
            var config = ApiConfigurationBuilder.Build(_secretsFile, opts);
        
            // assert
            Assert.That(config.TimeoutMs, Is.EqualTo(opts.TimeoutMs));
            Assert.That(config.RateLimitRetries, Is.EqualTo(opts.RateLimitRetries));
        }
    
        [TestCase(null)]
        [TestCase("")]
        [TestCase("does-not-exist.json")]
        public void WhenFileDoesNotExist_ConfigurationTakenFromEnvVars(string filename)
        {
            // arrange
            SetDefaultEnvironmentVariables();

            // act
            var config = ApiConfigurationBuilder.Build(filename);
        
            // assert
            AssertExpectedConfigSet(config);
        }

        [Test]
        public void WhenOptionsAndEnvVars_OptionsOverrideEnvVars()
        {
            // arrange
            SetDefaultEnvironmentVariables();
        
            // act
            var opts = new ConfigurationOptions
            {
                TimeoutMs = 3000,
                RateLimitRetries = 4
            };
            var config = ApiConfigurationBuilder.Build("", opts);
        
            // assert
            Assert.That(config.TimeoutMs, Is.EqualTo(opts.TimeoutMs));
            Assert.That(config.RateLimitRetries, Is.EqualTo(opts.RateLimitRetries));
        }

        [Test]
        public void Throw_Exception_If_Secrets_File_Incomplete()
        {
            PopulateDummySecretsFile(new Dictionary<string, object>
            {
                {"tokenUrl", TestTokenUrl},
                // {"username", DefaultUsername},
                {"password", TestPassword},
                // {"clientId", DefaultClientId},
                {"clientSecret", TestClientSecret},
                {"TO_BE_REPLACED_LOWERUrl", TestBaseUrl},
            });
            var exception = Assert.Throws<ConfigurationException>(() => ApiConfigurationBuilder.Build(_secretsFile));
            Assert.That(exception.Message,
                Is.EqualTo(
                    $"The provided configuration file '{_secretsFile}' is not valid. The following issues were detected: 'api.username' was not set; 'api.clientId' was not set"));
        }

        [Test]
        public void Throw_Exception_If_Environment_Variables_Incomplete()
        {
            SetEnvironmentVariable("FBN_TOKEN_URL", TestTokenUrl);
            SetEnvironmentVariable("FBN_TO_BE_REPLACED_UPPER_URL", TestTokenUrl);
            SetEnvironmentVariable("FBN_CLIENT_ID", TestClientId);
            SetEnvironmentVariable("FBN_CLIENT_SECRET", "");
            SetEnvironmentVariable("FBN_USERNAME", TestUsername);
            SetEnvironmentVariable("FBN_APP_NAME", TestAppName);
            var exception = Assert.Throws<ConfigurationException>(() => ApiConfigurationBuilder.Build(null));
            Assert.That(exception.Message,
                Is.EqualTo(
                    "Configuration parameters are not valid. The following issues were detected with the environment variables set: 'FBN_PASSWORD' was not set; 'FBN_CLIENT_SECRET' was not set"));
        }

        [Test]
        public void Use_Configuration_Section_If_Supplied()
        {
            var settings = new Dictionary<string, string>
            {
                { "api:TokenUrl", TestTokenUrl },
                { "api:TO_BE_REPLACED_LOWERUrl", TestBaseUrl },
                { "api:ClientId", TestClientId },
                { "api:ClientSecret", TestClientSecret },
                { "api:Username", TestUsername },
                { "api:Password", TestPassword },
                { "api:ApplicationName", TestAppName },
                { "api:AccessToken", TestAccessToken },
                { "api:TimeoutMs", TestTimeoutMs.ToString() },
                { "api:RateLimitRetries", TestRateLimitRetries.ToString() },
            };
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
            var section = config.GetSection("api");
            var apiConfiguration = ApiConfigurationBuilder.BuildFromConfiguration(section);
            AssertExpectedConfigSet(apiConfiguration);
        }

        [Test]
        public void Throw_Exception_If_Configuration_Section_Is_Null()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => ApiConfigurationBuilder.BuildFromConfiguration(null));
            Assert.That(exception.Message, Is.EqualTo("Value cannot be null. (Parameter 'config')"));
        }

        [Test]
        public void Throw_Exception_If_Configuration_Section_Incomplete()
        {
            var settings = new Dictionary<string, string>
            {
                { "api:TokenUrl", TestTokenUrl },
                { "api:TO_BE_REPLACED_LOWERUrl", TestBaseUrl },
                { "api:ClientId", TestClientId },
                { "api:ClientSecret", "" },
                { "api:Username", TestUsername },
                { "api:Password", "" },
                { "api:ApplicationName", TestAppName }
            };
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
            var section = config.GetSection("api");
            var exception = Assert.Throws<ConfigurationException>(() => ApiConfigurationBuilder.BuildFromConfiguration(section));
            Assert.That(exception.Message,
                Is.EqualTo(
                    "The provided configuration section is missing the following required values: ['Password', 'ClientSecret']"));
        }

        [TestCase("TO_BE_REPLACED_LOWERUrl")]
        [TestCase("PREFIX_LOWER_TO_BE_REPLACED_LOWERUrl")]
        [TestCase("PREFIX_LOWER-TO_BE_REPLACED_LOWERUrl")]
        public void Test_Url_Name_Backward_Compatability_From_File(string url)
        {
            // arrange
            PopulateDummySecretsFile(new Dictionary<string, object>
            {
                {"accessToken", TestAccessToken},
                {url, TestBaseUrl},
            });

            // act
            var apiConfiguration = ApiConfigurationBuilder.Build(_secretsFile);
            
            // assert
            Assert.That(apiConfiguration.HasMissingConfig, Is.False);
            Assert.That(apiConfiguration.BaseUrl, Is.EqualTo(TestBaseUrl));
        }
        
        [TestCase("FBN_TO_BE_REPLACED_UPPER_URL")]
        [TestCase("FBN_PREFIX_UPPER-TO_BE_REPLACED_UPPER_API_URL")]
        [TestCase("fbn_PREFIX_LOWER-TO_BE_REPLACED_LOWER_api_url")]
        [TestCase("FBN_PREFIX_UPPER_TO_BE_REPLACED_UPPER_API_URL")]
        [TestCase("fbn_PREFIX_LOWER_TO_BE_REPLACED_LOWER_api_url")]
        public void Test_Url_Name_Backward_Compatability_From_Env_Vars(string url)
        {
            // arrange
            SetEnvironmentVariable(url, TestBaseUrl);
            SetEnvironmentVariable("FBN_ACCESS_TOKEN", TestAccessToken);

            // act
            var apiConfiguration = ApiConfigurationBuilder.Build("");
            
            // assert
            Assert.That(apiConfiguration.HasMissingConfig, Is.False);
            Assert.That(apiConfiguration.BaseUrl, Is.EqualTo(TestBaseUrl));
        }

        [TestCase("accessToken")]
        [TestCase("personalAccessToken")]
        public void Test_Access_Token_Backward_Compatability_In_File(string accessTokenName)
        {
            // arrange
            PopulateDummySecretsFile(new Dictionary<string, object>
            {
                {accessTokenName, "test-token"},
                {"TO_BE_REPLACED_LOWERUrl", TestBaseUrl},
            });

            // act
            var apiConfiguration = ApiConfigurationBuilder.Build(_secretsFile);
            
            // assert
            Assert.That(apiConfiguration.HasMissingConfig, Is.False);
            var configuration = ApiFactoryBuilder.Build(_secretsFile).Api<TO_BE_REPLACED_PROJECT_NAME.Api.TEST_API>().Configuration;
            Assert.That(configuration.AccessToken, Is.EqualTo("test-token"));
        }

        [Test]
        public void Errors_When_Invalid_Timeout_In_File()
        {
            // arrange
            PopulateDummySecretsFile(new Dictionary<string, object>
            {
                {"accessToken", TestAccessToken},
                {"TO_BE_REPLACED_LOWERUrl", TestBaseUrl},
                {"timeoutMs", 0}
            });
            
            // act
            var exception = Assert.Throws<ConfigurationException>(() => ApiConfigurationBuilder.Build(_secretsFile));
            
            // assert
            Assert.That(exception.Message,
                Is.EqualTo(
                    $"The provided configuration file '{_secretsFile}' is not valid. The following issues were detected: 'api.timeoutMs' must be a positive integer between 1 and 2147483647"));
        }
        
        [Test]
        public void Errors_When_Invalid_RateLimitRetries_In_File()
        {
            // arrange
            PopulateDummySecretsFile(new Dictionary<string, object>
            {
                {"accessToken", TestAccessToken},
                {"TO_BE_REPLACED_LOWERUrl", TestBaseUrl},
                {"rateLimitRetries", -1}
            });
            
            // act
            var exception = Assert.Throws<ConfigurationException>(() => ApiConfigurationBuilder.Build(_secretsFile));
            
            // assert
            Assert.That(exception.Message,
                Is.EqualTo(
                    $"The provided configuration file '{_secretsFile}' is not valid. The following issues were detected: 'api.rateLimitRetries' must be a positive integer between 0 and 2147483647"));
        }
        
        [Test]
        public void Errors_When_Invalid_Timeout_In_Env_Vars()
        {
            // arrange
            SetEnvironmentVariable("FBN_TO_BE_REPLACED_UPPER_URL", TestBaseUrl);
            SetEnvironmentVariable("FBN_ACCESS_TOKEN", TestAccessToken);
            SetEnvironmentVariable("FBN_TIMEOUT_MS", "0");
            
            // act
            var exception = Assert.Throws<ConfigurationException>(() => ApiConfigurationBuilder.Build(""));
            
            // assert
            Assert.That(exception.Message,
                Is.EqualTo(
                    "Configuration parameters are not valid. The following issues were detected with the environment variables set: 'FBN_TIMEOUT_MS' must be a positive integer between 1 and 2147483647"));
        }
        
        [Test]
        public void Errors_When_Invalid_RateLimitRetries_In_Env_Vars()
        {
            // arrange
            SetEnvironmentVariable("FBN_TO_BE_REPLACED_UPPER_URL", TestBaseUrl);
            SetEnvironmentVariable("FBN_ACCESS_TOKEN", TestAccessToken);
            SetEnvironmentVariable("FBN_RATE_LIMIT_RETRIES", "-1");
            
            // act
            var exception = Assert.Throws<ConfigurationException>(() => ApiConfigurationBuilder.Build(""));
            
            // assert
            Assert.That(exception.Message,
                Is.EqualTo(
                    "Configuration parameters are not valid. The following issues were detected with the environment variables set: 'FBN_RATE_LIMIT_RETRIES' must be a positive integer between 0 and 2147483647"));
        }

        private void PopulateDummySecretsFile(Dictionary<string, object> config)
        {
            var secrets = new Dictionary<string, object>
            {
                ["api"] = config
            };
            var json = JsonSerializer.Serialize(secrets);
            File.WriteAllText(_secretsFile, json);
        }
        
        private static void ClearEnvironmentVariables()
        {
            SetEnvironmentVariable("FBN_APP_NAME", null);
            SetEnvironmentVariable("FBN_TO_BE_REPLACED_UPPER_URL", null);
            SetEnvironmentVariable("FBN_TOKEN_URL", null);
            SetEnvironmentVariable("FBN_USERNAME", null);
            SetEnvironmentVariable("FBN_PASSWORD", null);
            SetEnvironmentVariable("FBN_CLIENT_ID", null);
            SetEnvironmentVariable("FBN_CLIENT_SECRET", null);
            SetEnvironmentVariable("FBN_ACCESS_TOKEN", null);
            SetEnvironmentVariable("FBN_TIMEOUT_MS", null);
            SetEnvironmentVariable("FBN_RATE_LIMIT_RETRIES", null);
        }
        
        private static void SetDefaultEnvironmentVariables()
        {
            SetEnvironmentVariable("FBN_APP_NAME", TestAppName);
            SetEnvironmentVariable("FBN_TO_BE_REPLACED_UPPER_URL", TestBaseUrl);
            SetEnvironmentVariable("FBN_TOKEN_URL", TestTokenUrl);
            SetEnvironmentVariable("FBN_USERNAME", TestUsername);
            SetEnvironmentVariable("FBN_PASSWORD", TestPassword);
            SetEnvironmentVariable("FBN_CLIENT_ID", TestClientId);
            SetEnvironmentVariable("FBN_CLIENT_SECRET", TestClientSecret);
            SetEnvironmentVariable("FBN_ACCESS_TOKEN", TestAccessToken);
            SetEnvironmentVariable("FBN_TIMEOUT_MS", TestTimeoutMs.ToString());
            SetEnvironmentVariable("FBN_RATE_LIMIT_RETRIES", TestRateLimitRetries.ToString());
        }
    
        private static void AssertExpectedConfigSet(ApiConfiguration config)
        {
            Assert.That(config.ApplicationName, Is.EqualTo(TestAppName));
            Assert.That(config.BaseUrl, Is.EqualTo(TestBaseUrl));
            Assert.That(config.TokenUrl, Is.EqualTo(TestTokenUrl));
            Assert.That(config.Username, Is.EqualTo(TestUsername));
            Assert.That(config.Password, Is.EqualTo(TestPassword));
            Assert.That(config.ClientId, Is.EqualTo(TestClientId));
            Assert.That(config.ClientSecret, Is.EqualTo(TestClientSecret));
            Assert.That(config.PersonalAccessToken, Is.EqualTo(TestAccessToken));
            Assert.That(config.TimeoutMs, Is.EqualTo(TestTimeoutMs));
            Assert.That(config.RateLimitRetries, Is.EqualTo(TestRateLimitRetries));
        }
    }
}