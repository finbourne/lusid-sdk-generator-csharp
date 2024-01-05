using NUnit.Framework;
using System;

namespace Finbourne.Sdk.Extensions.Tests.Integration
{
    [TestFixture]
    public class TokenProviderConfigurationTest
    {
        private static readonly Lazy<ApiConfiguration> ApiConfig =
            new Lazy<ApiConfiguration>(() => ApiConfigurationBuilder.Build("secrets.json"));

        //Test requires [assembly: InternalsVisibleTo("namespace Finbourne.Sdk.Extensions.IntegrationTests")] in SDK project.
        [Test]
        public void Construct_AccessToken_OIDC_NonNull()
        {
            ITokenProvider tokenProvider = new ClientCredentialsFlowTokenProvider(ApiConfigurationBuilder.Build("secrets.json"));

            var config = new TokenProviderConfiguration(tokenProvider);
            Assert.IsNotNull(config.AccessToken);
        }

                [Test]
        public void Construct_AccessToken_PAT_NonNull()
        {
            ITokenProvider tokenProvider = new PersonalAccessTokenProvider(ApiConfig.Value.PersonalAccessToken);
            var config = new TokenProviderConfiguration(tokenProvider);
            Assert.IsNotNull(config.AccessToken);
        }
    }
}
