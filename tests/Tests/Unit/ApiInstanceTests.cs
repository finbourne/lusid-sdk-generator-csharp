using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using TO_BE_REPLACED_PROJECT_NAME.Api;
using RestSharp;
using NUnit.Framework;
// this is necessary due to the name conflict in the Configurations sdk
using SdkConfiguration = TO_BE_REPLACED_PROJECT_NAME.Client.Configuration;

namespace Finbourne.Sdk.Extensions.Tests.Unit;

public class ApiInstanceTests
{
    private class TestRestClient : RestClient, IRestClientWrapper
    {
        private readonly HttpStatusCode _returnStatusCode;
        public readonly List<RestRequest> Requests = new();

        public TestRestClient(HttpStatusCode returnStatusCode)
        {
            _returnStatusCode = returnStatusCode;
        }

        public RestResponse WrappedExecute(RestRequest request)
        {
            Requests.Add(request);
            return new RestResponse
            {
                StatusCode = _returnStatusCode,
            };
        }

        public RestResponse<T> WrappedExecute<T>(RestRequest request)
        {
            Requests.Add(request);
            return new RestResponse<T>(request)
            {
                StatusCode = _returnStatusCode,
            };
        }

        public new System.Threading.Tasks.Task<RestResponse> ExecuteAsync(RestRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            Requests.Add(request);
            return System.Threading.Tasks.Task.FromResult(new RestResponse(request)
            {
                StatusCode = _returnStatusCode,
            });
        }
    }
    
    [Test]
    public void WhenNoTimeoutSet_DefaultTimeoutSetOnRequest()
    {
        // arrange
        var testRestClient = new TestRestClient(HttpStatusCode.OK);
        var apiClient = GetApiClient(
            expectedRestClientTimeout: SdkConfiguration.DefaultTimeoutMs, 
            expectedConfigurationTimeout: SdkConfiguration.DefaultTimeoutMs, 
            testRestClient);
        var configuration = new SdkConfiguration();
        var apiInstance = new TEST_API(apiClient, apiClient, configuration);
        
        // act
        apiInstance.TEST_METHOD);
        
        // assert
        Assert.That(testRestClient.Requests.Count, Is.EqualTo(1));
        var request = testRestClient.Requests.Single();
        Assert.That(request.Timeout, Is.EqualTo(SdkConfiguration.DefaultTimeoutMs));
    }

    [Test]
    public void WhenTimeoutSetInConfiguration_TimeoutSetOnRequest()
    {
        // arrange
        var testRestClient = new TestRestClient(HttpStatusCode.OK);
        var configuration = new SdkConfiguration
        {
            TimeoutMs = 10000
        };
        var apiClient = GetApiClient(
            expectedRestClientTimeout: configuration.TimeoutMs, 
            expectedConfigurationTimeout: configuration.TimeoutMs, 
            testRestClient);
        var apiInstance = new TEST_API(apiClient, apiClient, configuration);
        
        // act
        apiInstance.TEST_METHOD);
        
        // assert
        Assert.That(testRestClient.Requests.Count, Is.EqualTo(1));
        var request = testRestClient.Requests.Single();
        Assert.That(request.Timeout, Is.EqualTo(configuration.TimeoutMs));
    }

    [Test]
    public void WhenTimeoutOverriddenOnSyncCall_TimeoutSetOnRequest()
    {
        // arrange
        var testRestClient = new TestRestClient(HttpStatusCode.OK);
        var configuration = new SdkConfiguration
        {
            TimeoutMs = 10000
        };
        var opts = new ConfigurationOptions
        {
            TimeoutMs = 20000
        };
        var apiClient = GetApiClient(
            expectedRestClientTimeout: opts.TimeoutMs.Value, 
            expectedConfigurationTimeout: configuration.TimeoutMs, 
            testRestClient);
        var apiInstance = new TEST_API(apiClient, apiClient, configuration);
        
        // act
        apiInstance.TEST_METHOD_WITH_EXTRA_ARGopts: opts);
        
        // assert
        Assert.That(testRestClient.Requests.Count, Is.EqualTo(1));
        var request = testRestClient.Requests.Single();
        Assert.That(request.Timeout, Is.EqualTo(opts.TimeoutMs));
    }
    
    [Test]
    public async System.Threading.Tasks.Task WhenTimeoutOveriddenOnAsyncCall_TimeoutSetOnRequest()
    {
        // arrange
        var testRestClient = new TestRestClient(HttpStatusCode.OK);
        var configuration = new SdkConfiguration
        {
            TimeoutMs = 10000
        };
        var opts = new ConfigurationOptions
        {
            TimeoutMs = 20000
        };
        var apiClient = GetApiClient(
            expectedRestClientTimeout: opts.TimeoutMs.Value, 
            expectedConfigurationTimeout: configuration.TimeoutMs, 
            testRestClient);
        var apiInstance = new TEST_API(apiClient, apiClient, configuration);
        
        // act
        await apiInstance.ASYNC_TEST_METHOD_WITH_EXTRA_ARGopts: opts);
        
        // assert
        Assert.That(testRestClient.Requests.Count, Is.EqualTo(1));
        var request = testRestClient.Requests.Single();
        Assert.That(request.Timeout, Is.EqualTo(opts.TimeoutMs));
    }
    
    private static ApiClient GetApiClient(
        int expectedRestClientTimeout, 
        int expectedConfigurationTimeout, 
        TestRestClient testRestClient)
    {
        var apiClient = new ApiClient("http://localhost", createRestClientFunc: (options, readableConfiguration) =>
        {
            Assert.That(options.MaxTimeout, Is.EqualTo(expectedRestClientTimeout));
            Assert.That(readableConfiguration.TimeoutMs, Is.EqualTo(expectedConfigurationTimeout));
            return testRestClient;
        });
        return apiClient;
    }

    [Test]
    public void WhenNoRateLimitRetriesSet_DefaultRateLimitRetriesConfigured()
    {
        var configuration = new SdkConfiguration();
        var expectedNumberOfAttempts = SdkConfiguration.DefaultRateLimitRetries;
        VerifyRateLimitRetriesRespected(configuration, expectedNumberOfAttempts);
    }

    [Test]
    public void WhenRateLimitRetriesSet_RateLimitRetriesRespected()
    {
        var configuration = new SdkConfiguration
        {
            RateLimitRetries = 1
        };
        var expectedNumberOfRetries = configuration.RateLimitRetries;
        VerifyRateLimitRetriesRespected(configuration, expectedNumberOfRetries);
    }

    private static void VerifyRateLimitRetriesRespected(SdkConfiguration configuration, int expectedNumberOfRetries)
    {
        // arrange
        var testRestClient = new TestRestClient(HttpStatusCode.TooManyRequests);
        var apiClient = new ApiClient("http://localhost", createRestClientFunc: (options, readableConfiguration) =>
        {
            Assert.That(readableConfiguration.RateLimitRetries, Is.EqualTo(expectedNumberOfRetries));
            return testRestClient;
        });
        var apiInstance = new TEST_API(apiClient, apiClient, configuration);
        RetryConfiguration.GetRetryPolicyFunc = requestOptions =>
        {
            var rateLimitRetries = requestOptions.RateLimitRetries ?? configuration.RateLimitRetries;
            return PollyApiRetryHandler.GetDefaultRetryPolicyWithRateLimitWithFallback(rateLimitRetries);
        };
        
        // act
        Assert.Throws<ApiException>(() => apiInstance.TEST_METHOD));
        
        // assert
        Assert.That(testRestClient.Requests.Count, Is.EqualTo(expectedNumberOfRetries + 1));
    }
}