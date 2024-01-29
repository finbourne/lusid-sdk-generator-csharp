using System;
using NUnit.Framework;
using Lusid.Sdk.Api;
using System.Net.Http;
using System.Threading;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Finbourne.Sdk.Extensions.Tests.Unit
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public MockHttpMessageHandler(HttpResponseMessage response){
            this.response = response;
        }
        public HttpResponseMessage response;
        public HttpRequestMessage requestMessage {get;set;}
        async protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            requestMessage = request;
            return response;
        }
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            requestMessage = request;
            return response;
        }
    }
    public class SampleApiTest
    {
        [Test]
        async public Task TestGetTransactionTypeEncodesRequestCorrectly(){
            var expectedResponse = new HttpResponseMessage(){
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new TransactionType(aliases: new List<TransactionTypeAlias>(), movements: new List<TransactionTypeMovement>()))
            };
            var httpMessageHandlerMock = new MockHttpMessageHandler(expectedResponse);
            var apiClient = new ApiClient("http://example.com", (options)=>httpMessageHandlerMock);
            var api = new TransactionConfigurationApi(apiClient, apiClient, new Configuration());
            api.GetTransactionType(source:"here", type:"trombone");
            Assert.AreEqual(httpMessageHandlerMock.requestMessage.RequestUri, "http://example.com/api/transactionconfiguration/types/here/trombone");
        }
        
    }
}
