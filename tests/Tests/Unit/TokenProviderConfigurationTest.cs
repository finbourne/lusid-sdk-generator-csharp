using NUnit.Framework;

namespace Finbourne.Sdk.Extensions.Tests.Unit
{
    [TestFixture]
    public class TokenProviderConfigurationTest
    {
        [Test]
        public void Construct_WithNullTokenProvider_Returns_NonNull()
        {
            var config = new TokenProviderConfiguration(null);
            Assert.IsNotNull(config);
        }

        [Test]
        public void Construct_WithNullTokenProvider_Returns_BasePathSet()
        {
            var config = new TokenProviderConfiguration(null);
            StringAssert.Contains($".lusid.com/", config.BasePath);
        }
    }
}
