using NUnit.Framework;

namespace Finbourne.Sdk.Extensions.Tests.Unit
{
    [TestFixture]
    public class MissingConfigExceptionTest
    {
        [Test]
        public void Construct_Check_Message()
        {
            var cfgEx = new MissingConfigException("missing-config-exception");
            Assert.AreEqual("missing-config-exception", cfgEx.Message);
        }
    }
}
