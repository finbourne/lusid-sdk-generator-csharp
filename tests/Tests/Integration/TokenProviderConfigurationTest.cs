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
        public void Construct_AccessToken_NonNull()
        {
            ITokenProvider tokenProvider;
            if (ApiConfig.Value.MissingSecretVariables)
            {
                tokenProvider = new PersonalAccessTokenProvider(ApiConfig.Value.PersonalAccessToken);
            }
            else
            {
                tokenProvider = new ClientCredentialsFlowTokenProvider(ApiConfigurationBuilder.Build("secrets.json"));
            }

            var config = new TokenProviderConfiguration(tokenProvider);
            Assert.IsNotNull(config.AccessToken);
        }
    }
}
