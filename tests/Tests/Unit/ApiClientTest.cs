using System;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Moq;
using Moq.Protected;
using System.Net;
using RestSharp;
using System.Collections.Generic;
using System.Text;

namespace Finbourne.Sdk.Extensions.Tests.Unit
{
    [TestFixture]
    public class ApiClientTest
    {
        /// <summary>
        /// Test to ensure that slashes that have been properly escaped in requests
        /// are sent to the server unmodified.
        /// Workaround for slashes in segment parameters not being
        /// supported in RestSharp
        /// https://github.com/restsharp/RestSharp/issues/707#issuecomment-636462446
        /// https://github.com/restsharp/RestSharp/blob/dev/src/RestSharp/Parameters/UrlSegmentParameter.cs#L26
        /// 
        /// %2F should be encoded to %252F
        /// PREVIOUS BEHAVIOUR: %2F -> %2F
        /// </summary>
        [Test]
        public void PercentEncodedSlashSent(){
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handler.Protected().Setup<Task<HttpResponseMessage>>(
                    "SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) => new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(req.RequestUri!.AbsolutePath ?? "oops")
                });
            Func<RestClientOptions, HttpMessageHandler> HttpClientFactoryMock = (options) => handler.Object;
            var apiClient = new ApiClient("http://example.com", CreateHttpMessageHandler : HttpClientFactoryMock);
            var requestOptions = new RequestOptions(){
                Operation = "GET",
                PathParameters=new Dictionary<string, string>(){
                    {"arg", "prefix-%2F-suffix"}
                }
            };
            var res = apiClient.Get<String>("/{arg}", requestOptions);
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual("/prefix-%252F-suffix", res.Content);
        }
        /// <summary>
        /// Test to ensure several different strings are encoded correctly
        /// when sent as url segment parameters.
        /// </summary>
        [Test]
        public void UrlSegmentParameterEncodings(){
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handler.Protected().Setup<Task<HttpResponseMessage>>(
                    "SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) => new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(req.RequestUri!.AbsolutePath ?? "oops")
                });
            Func<RestClientOptions, HttpMessageHandler> HttpClientFactoryMock = (options) => handler.Object;
            var apiClient = new ApiClient("http://example.com", CreateHttpMessageHandler : HttpClientFactoryMock);
            var requestOptions = new RequestOptions(){
                Operation = "GET",
                PathParameters=new Dictionary<string, string>(){
                    {"plus", "prefix-+-suffix"},
                    {"space", "prefix- -suffix"},
                    {"percent24", "prefix-%24-suffix"}
                }
            };
            var res = apiClient.Get<String>("/{plus}/{space}/{percent24}", requestOptions);
            var expectedPath = new StringBuilder()
                .Append("/prefix-%2B-suffix")
                .Append("/prefix-%20-suffix")
                .Append("/prefix-%2524-suffix")
                .ToString();
            
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual(expectedPath, res.Content);
        }
    }
}
