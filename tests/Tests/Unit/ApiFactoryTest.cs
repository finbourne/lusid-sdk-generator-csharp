using TO_BE_REPLACED_PROJECT_NAME.Api;
using NUnit.Framework;
using System;
using SdkConfiguration = TO_BE_REPLACED_PROJECT_NAME.Client.Configuration;

namespace Finbourne.Sdk.Extensions.Tests.Unit
{
    [TestFixture]
    public class ApiFactoryTest
    {
        [SetUp]
        public void SetUp()
        {
            // reset the global static state
            GlobalConfiguration.SetDefaultInstance();
        }
        
        [TearDown]
        public void TearDown()
        {
            // reset the global static state
            GlobalConfiguration.SetDefaultInstance();
        }
        
        [Test]
        public void WhenConstructedWithApiConfiguration_AndNoOptionalValuesSet_AndDefaultGlobalConfig_DefaultConfigUsed()
        {
            // arrange
            var apiConfiguration = new ApiConfiguration
            {
                PersonalAccessToken = "token",
                BaseUrl = "http://localhost"
            };
            var apiFactory = new ApiFactory(apiConfiguration);
            
            // act
            var configuration = apiFactory.Api<TEST_API>().Configuration;
            
            // assert
            Assert.That(configuration.TimeoutMs, Is.EqualTo(SdkConfiguration.DefaultTimeoutMs));
            Assert.That(configuration.RateLimitRetries, Is.EqualTo(SdkConfiguration.DefaultRateLimitRetries));
        }
        
        [Test]
        public void WhenConstructedWithApiConfiguration_AndNoOptionalValuesSet_AndGlobalConfigSet_ValuesFromGlobalConfigUsed()
        {
            // arrange
            var apiConfiguration = new ApiConfiguration
            {
                PersonalAccessToken = "token",
                BaseUrl = "http://localhost"
            };
            GlobalConfiguration.Instance = new SdkConfiguration
            {
                TimeoutMs = 1001,
                RateLimitRetries = 7
            };
            var apiFactory = new ApiFactory(apiConfiguration);
            
            // act
            var configuration = apiFactory.Api<TEST_API>().Configuration;
            
            // assert
            Assert.That(configuration.TimeoutMs, Is.EqualTo(1001));
            Assert.That(configuration.RateLimitRetries, Is.EqualTo(7));
        }

        [Test]
        public void WhenConstructedWithApiConfiguration_AndOptionalValuesSet_AndDefaultGlobalConfig_ValuesFromApiConfigurationUsed()
        {
            // arrange
            var apiConfiguration = new ApiConfiguration
            {
                PersonalAccessToken = "token",
                BaseUrl = "http://localhost",
                TimeoutMs = 2000,
                RateLimitRetries = 6
            };
            var apiFactory = new ApiFactory(apiConfiguration);
            
            // act
            var configuration = apiFactory.Api<TEST_API>().Configuration;
            
            // assert
            Assert.That(configuration.TimeoutMs, Is.EqualTo(2000));
            Assert.That(configuration.RateLimitRetries, Is.EqualTo(6));
        }

        [Test]
        public void WhenConstructedWithApiConfiguration_AndOptionalValuesSet_AndGlobalConfigSet_ValuesFromApiConfigurationUsed()
        {
            // arrange
            var apiConfiguration = new ApiConfiguration
            {
                PersonalAccessToken = "token",
                BaseUrl = "http://localhost",
                TimeoutMs = 2000,
                RateLimitRetries = 6
            };
            GlobalConfiguration.Instance = new SdkConfiguration
            {
                TimeoutMs = 1002,
                RateLimitRetries = 7
            };
            var apiFactory = new ApiFactory(apiConfiguration);
            
            // act
            var configuration = apiFactory.Api<TEST_API>().Configuration;
            
            // assert
            Assert.That(configuration.TimeoutMs, Is.EqualTo(2000));
            Assert.That(configuration.RateLimitRetries, Is.EqualTo(6));
        }
        
        [Test]
        public void WhenConstructedWithDefaultConfiguration_AndDefaultGlobalConfig_DefaultValuesUsed()
        {
            // arrange
            var originalConfiguration = new SdkConfiguration();
            var apiFactory = new ApiFactory(originalConfiguration);
            
            // act
            var configuration = apiFactory.Api<TEST_API>().Configuration;
            
            // assert
            Assert.That(configuration.TimeoutMs, Is.EqualTo(SdkConfiguration.DefaultTimeoutMs));
            Assert.That(configuration.RateLimitRetries, Is.EqualTo(SdkConfiguration.DefaultRateLimitRetries));
        }
        
        [Test]
        public void WhenConstructedWithConfiguration_AndDefaultGlobalConfig_ValuesFromConfigurationUsed()
        {
            // arrange
            var originalConfiguration = new SdkConfiguration
            {
                TimeoutMs = 3000,
                RateLimitRetries = 5
            };
            var apiFactory = new ApiFactory(originalConfiguration);
            
            // act
            var configuration = apiFactory.Api<TEST_API>().Configuration;
            
            // assert
            Assert.That(configuration.TimeoutMs, Is.EqualTo(3000));
            Assert.That(configuration.RateLimitRetries, Is.EqualTo(5));
        } 

        [Test]
        public void WhenConstructedWithConfiguration_AndGlobalConfigSet_ValuesFromConfigurationUsed()
        {
            // arrange
            var originalConfiguration = new SdkConfiguration
            {
                TimeoutMs = 3000,
                RateLimitRetries = 5
            };
            GlobalConfiguration.Instance = new SdkConfiguration
            {
                TimeoutMs = 1003,
                RateLimitRetries = 7
            };
            var apiFactory = new ApiFactory(originalConfiguration);
            
            // act
            var configuration = apiFactory.Api<TEST_API>().Configuration;
            
            // assert
            Assert.That(configuration.TimeoutMs, Is.EqualTo(3000));
            Assert.That(configuration.RateLimitRetries, Is.EqualTo(5));
        } 
    
        [Test]
        public void InvalidTokenUrl_ThrowsException()
        {
            ApiConfiguration apiConfig = new ApiConfiguration
            {
                TokenUrl = "xyz"
            };

            Assert.That(
                () => new ApiFactory(apiConfig),
                Throws.InstanceOf<UriFormatException>().With.Message.EqualTo("Invalid Token Uri: xyz"));
        }

        [Test]
        public void InvalidbaseUrl_ThrowsException()
        {
            ApiConfiguration apiConfig = new ApiConfiguration
            {
                TokenUrl = "http://finbourne.com",
                BaseUrl = "xyz"
            };

            Assert.That(
                () => new ApiFactory(apiConfig),
                Throws.InstanceOf<UriFormatException>().With.Message.EqualTo("Invalid Uri: xyz"));
        }

        [Test]
        public void NullAppName_NoLusidApplicationHeaderInApiConfig()
        {
            var apiConfig = new ApiConfiguration()
            {
                PersonalAccessToken = "test token",
                BaseUrl = "https://example.lusid.com/api"
            };
            var apiFactory = new ApiFactory(apiConfig);
            Assert.That(apiFactory.Api<TEST_API>().Configuration.DefaultHeaders, Does.Not.ContainKey("X-LUSID-Application"));
        }

        [Test]
        public void AppNameProvided_LusidApplicationHeaderInApiConfig()
        {
            var appName = "test app name";
            var apiConfig = new ApiConfiguration()
            {
                PersonalAccessToken = "test token",
                BaseUrl = "https://example.lusid.com/api",
                ApplicationName = appName
            };
            var apiFactory = new ApiFactory(apiConfig);
            Assert.That(apiFactory.Api<TEST_API>().Configuration.DefaultHeaders, Does.ContainKey("X-LUSID-Application"));
            Assert.AreEqual(appName, apiFactory.Api<TEST_API>().Configuration.DefaultHeaders["X-LUSID-Application"]);
        }
    }
}
