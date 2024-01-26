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
            var httpClient = new HttpClient(handler.Object);
            var HttpClientFactoryMock = new Mock<IHttpClientFactory>();
            HttpClientFactoryMock.Setup(httpClientFactory=>httpClientFactory.New(It.IsAny<RestClientOptions>())).Returns(httpClient);
            var apiClient = new ApiClient("http://example.com");
            var requestOptions = new RequestOptions(){
                Operation = "GET",
                PathParameters=new Dictionary<string, string>(){
                    {"arg", "prefix-%2F-suffix"}
                }
            };
            var config = new Configuration(){
                HttpClientFactory = HttpClientFactoryMock.Object
            };
            var res = apiClient.Get<String>("/{arg}", requestOptions, configuration:config);

            var req = new RestRequest(new Uri("http://example.com/{arg}"));
            
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual("/prefix-%252F-suffix", res.Content);
        }
        
    }
}
