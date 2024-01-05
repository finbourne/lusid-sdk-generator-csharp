using Lusid.Sdk.Api;
using NUnit.Framework;
using System;

namespace Finbourne.Sdk.Extensions.Tests.Unit
{
    [TestFixture]
    public class ApiFactoryTest
    {
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
            Assert.That(apiFactory.Api<ApplicationMetadataApi>().Configuration.DefaultHeaders, Does.Not.ContainKey("X-LUSID-Application"));
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
            Assert.That(apiFactory.Api<ApplicationMetadataApi>().Configuration.DefaultHeaders, Does.ContainKey("X-LUSID-Application"));
            Assert.AreEqual(appName, apiFactory.Api<ApplicationMetadataApi>().Configuration.DefaultHeaders["X-LUSID-Application"]);
        }
    }
}
