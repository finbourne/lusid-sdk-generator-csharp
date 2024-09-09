using NUnit.Framework;

namespace Finbourne.Sdk.Extensions.Tests.Unit
{
    [TestFixture]
    public class ConfigurationExceptionTest
    {
        [Test]
        public void Construct_Check_Message()
        {
            var cfgEx = new ConfigurationException("missing-config");
            Assert.AreEqual("missing-config", cfgEx.Message);
        }
    }
}
