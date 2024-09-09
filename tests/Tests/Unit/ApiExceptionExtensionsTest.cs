using Newtonsoft.Json;
using NUnit.Framework;
using System.Net;

namespace Finbourne.Sdk.Extensions.Tests.Unit
{
    [TestFixture]
    public class ApiExceptionExtensionsTest
    {
        [Test]
        public void ProblemDetails_ApiException_ErrorContent_Null_Returns_Null()
        {
            ApiException apiEx = new ApiException((int)HttpStatusCode.BadGateway, "message");
            Assert.IsNull(apiEx.ProblemDetails());
        }

        [Test]
        public void ProblemDetails_ApiException_ErrorContent_Invalid_Returns_Null()
        {
            ApiException apiEx = new ApiException((int)HttpStatusCode.BadGateway, "message", errorContent: "invalid_error_content");
            Assert.IsNull(apiEx.ProblemDetails());
        }

        [Test]
        public void GetRequestId_ApiException_ProblemDetails_Null_Returns_Null()
        {
            ApiException apiEx = new ApiException((int)HttpStatusCode.BadGateway, "message", errorContent: "invalid_error_content");
            Assert.IsNull(apiEx.GetRequestId());
        }

        [Test]
        public void GetRequestId_WhenExceptionDoesNotContainRequestId_DoesNotThrow()
        {
            var exception = new ApiException(
                errorCode: 123,
                message: "Some Critical Exception",
                errorContent: JsonConvert.SerializeObject(new LusidProblemDetails(name: "CriticalException")));

            Assert.That(exception.GetRequestId(), Is.Null);
        }

        [Test]
        public void GetRequestId_WhenErrorContentIsNotAValidJson_DoesNotThrow()
        {
            const string errorContent = "<Some Invalid Json>";
            var exception = new ApiException(
                errorCode: 123,
                message: "Some Critical Exception",
                errorContent: errorContent);

            Assert.That(exception.GetRequestId(), Is.Null);
        }

        [Test]
        public void ProblemDetails_WhenErrorContentIsNotAValidJson_DoesNotThrow()
        {
            const string errorContent = "<Some Invalid Json>";
            var exception = new ApiException(
                errorCode: 123,
                message: "Some Critical Exception",
                errorContent: errorContent);

            Assert.That(exception.ProblemDetails(), Is.Null);
        }

        [Test]
        public void ApiException_Without_ErrorContent_Returns_Null()
        {
            var error = new ApiException();
            var errorResponse = error.ProblemDetails();

            Assert.That(errorResponse, Is.Null);
        }

        [Test]
        public void ApiException_Without_ErrorContent_Returns_NullRequestId()
        {
            var error = new ApiException();
            var errorResponse = error.GetRequestId();

            Assert.That(errorResponse, Is.Null);
        }

        [Test]
        public void ApiException_With_Empty_ErrorContent_Returns_Null()
        {
            var error = new ApiException();
            var errorResponse = error.ProblemDetails();

            Assert.That(errorResponse, Is.Null);
        }
    }
}
