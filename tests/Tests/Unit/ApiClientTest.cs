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

namespace Finbourne.Sdk.Extensions.Tests.Unit
{
    [TestFixture]
    public class ApiClientTest
    {

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

            var req = new RestRequest(new Uri("http://example.com/{arg}"));
            
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual("/prefix-%252F-suffix", res.Content);
        }
        
    }
}
