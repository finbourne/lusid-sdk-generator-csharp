using System;
using System.Net;
using TO_BE_REPLACED_PROJECT_NAME.Api;
using NUnit.Framework;
using Polly;
using RestSharp;
using SdkConfiguration = TO_BE_REPLACED_PROJECT_NAME.Client.Configuration;

namespace Finbourne.Sdk.Extensions.Tests.Unit
{
    [TestFixture]
    public class PollyApiRetryHandlerTest
    {
        [SetUp]
        public void SetUp()
        {
            RetryConfiguration.RetryPolicy = null;
            RetryConfiguration.GetRetryPolicyFunc = null;
            RetryConfiguration.AsyncRetryPolicy = null;
            RetryConfiguration.GetAsyncRetryPolicyFunc = null;
        }
        
        [TearDown]
        public void TearDown()
        {
            RetryConfiguration.RetryPolicy = null;
            RetryConfiguration.GetRetryPolicyFunc = null;
            RetryConfiguration.AsyncRetryPolicy = null;
            RetryConfiguration.GetAsyncRetryPolicyFunc = null;
        }

        [Test]
        public void SyncRequest_WhenNoRetryConfigurationSet_ByDefaultWillRetryOnConflict()
        {
            var statusCode = HttpStatusCode.Conflict;
            var expectedTimesCalled = PollyApiRetryHandler.DefaultNumberOfRetries + 1;

            AssertExpectedSyncPolicyBehaviour(statusCode, expectedTimesCalled);
        }
        
        [Test]
        public void SyncRequest_WhenNoRetryConfigurationSet_ByDefaultWillRetryOnRateLimit()
        {
            var statusCode = HttpStatusCode.TooManyRequests;
            var expectedTimesCalled = SdkConfiguration.DefaultRateLimitRetries + 1;

            AssertExpectedSyncPolicyBehaviour(statusCode, expectedTimesCalled);
        }

        private static void AssertExpectedSyncPolicyBehaviour(HttpStatusCode statusCode, int expectedTimesCalled)
        {
            // act
            new ApiFactory(new SdkConfiguration());
            var requestOptions = new RequestOptions();
            Policy<RestResponse> policy = ApiClient.GetSyncPolicy<TEST_API>(requestOptions);
            
            // assert
            Assert.That(policy, Is.Not.Null);
            var timesCalled = 0;
            var result = policy.ExecuteAndCapture(() =>
            {
                timesCalled += 1;
                return new RestResponse
                {
                    StatusCode = statusCode
                };
            });
            Assert.That(timesCalled, Is.EqualTo(expectedTimesCalled));
            Assert.That(result.Result.StatusCode, Is.EqualTo(statusCode));
        }

        [Test]
        public void SyncRequest_WhenRetryPolicyIsAlreadyAssigned_ExistingRetryPolicyIsUsed()
        {
            // arrange
            var testPolicy = Policy.HandleResult<RestResponse>(response => true).Retry();
            RetryConfiguration.RetryPolicy = testPolicy;
            
            // act
            new ApiFactory(new SdkConfiguration());

            // assert
            var policy = ApiClient.GetSyncPolicy<RestResponse>(new RequestOptions());
            Assert.That(policy, Is.EqualTo(testPolicy));
        }
        
        [Test]
        public void SyncRequest_RetryPolicyOverridesGetRetryPolicyFunc()
        {
            // arrange
            var testPolicy1 = Policy.HandleResult<RestResponse>(_ => true).Retry();
            var testPolicy2 = Policy.HandleResult<RestResponse>(_ => true).Retry();

            // when both these properties are set, AsyncRetryPolicy should take precedence
            RetryConfiguration.RetryPolicy = testPolicy1;
            RetryConfiguration.GetRetryPolicyFunc = (Func<RequestOptions, Policy<RestResponse>>)(_ => testPolicy2);

            // act
            new ApiFactory(new SdkConfiguration());
            
            // assert
            Assert.That(testPolicy1, Is.Not.EqualTo(testPolicy2));
            Assert.That(ApiClient.GetSyncPolicy<RestResponse>(new RequestOptions()), Is.EqualTo(testPolicy1));
        }
        
        [Test]
        public void SyncRequest_WhenRetryPolicyNull_UsesGetRetryPolicyFunc()
        {
            // arrange
            var testPolicy2 = Policy.HandleResult<RestResponse>(_ => true).Retry();
            RetryConfiguration.GetRetryPolicyFunc = (Func<RequestOptions, Policy<RestResponse>>)(_ => testPolicy2);
            
            // act
            new ApiFactory(new SdkConfiguration());
            
            // assert
            var syncPolicy = ApiClient.GetSyncPolicy<RestResponse>(new RequestOptions());
            Assert.That(syncPolicy, Is.EqualTo(testPolicy2));
        }

        [Test]
        public async System.Threading.Tasks.Task AsyncRequest_WhenNoRetryConfigurationSet_ByDefaultWillRetryOnConflict()
        {
            var statusCode = HttpStatusCode.Conflict;
            var expectedTimesCalled = PollyApiRetryHandler.DefaultNumberOfRetries + 1;
            
            await AssertExpectedAsyncPolicyBehaviour(statusCode, expectedTimesCalled);
        }
        
        [Test]
        public async System.Threading.Tasks.Task AsyncRequest_WhenNoRetryConfigurationSet_ByDefaultWillRetryOnRateLimit()
        {
            var statusCode = HttpStatusCode.TooManyRequests;
            var expectedTimesCalled = SdkConfiguration.DefaultRateLimitRetries + 1;
            
            await AssertExpectedAsyncPolicyBehaviour(statusCode, expectedTimesCalled);
        }

        private static async System.Threading.Tasks.Task AssertExpectedAsyncPolicyBehaviour(HttpStatusCode statusCode, int expectedTimesCalled)
        {
            // act
            new ApiFactory(new SdkConfiguration());
            var requestOptions = new RequestOptions();
            AsyncPolicy<RestResponse> policy = ApiClient.GetAsyncPolicy<TEST_API>(requestOptions);
            
            // assert
            Assert.That(policy, Is.Not.Null);
            var timesCalled = 0;
            var result = await policy.ExecuteAsync(() =>
            {
                timesCalled += 1;
                return System.Threading.Tasks.Task.FromResult(new RestResponse
                {
                    StatusCode = statusCode
                });
            });
            Assert.That(timesCalled, Is.EqualTo(expectedTimesCalled));
            Assert.That(result.StatusCode, Is.EqualTo(statusCode));
        }

        [Test]
        public void AsyncRequest_WhenRetryPolicyIsAlreadyAssigned_ExistingAsyncRetryPolicyIsUsed()
        {
            // arrange
            var testPolicy = Policy.HandleResult<RestResponse>(response => true).RetryAsync();
            RetryConfiguration.AsyncRetryPolicy = testPolicy;
            
            // act
            new ApiFactory(new SdkConfiguration());

            // assert
            var policy = ApiClient.GetAsyncPolicy<RestResponse>(new RequestOptions());
            Assert.That(policy, Is.EqualTo(testPolicy));
        }

        [Test]
        public void AsyncRequest_AsyncRetryPolicyOverridesGetAsyncRetryPolicyFunc()
        {
            // arrange
            var testPolicy1 = Policy.HandleResult<RestResponse>(_ => true).RetryAsync();
            var testPolicy2 = Policy.HandleResult<RestResponse>(_ => true).RetryAsync();

            // when both these properties are set, AsyncRetryPolicy should take precedence
            RetryConfiguration.AsyncRetryPolicy = testPolicy1;
            RetryConfiguration.GetAsyncRetryPolicyFunc = (Func<RequestOptions, AsyncPolicy<RestResponse>>)(_ => testPolicy2);

            // act
            new ApiFactory(new SdkConfiguration());
            
            // assert
            Assert.That(testPolicy1, Is.Not.EqualTo(testPolicy2));
            Assert.That(ApiClient.GetAsyncPolicy<RestResponse>(new RequestOptions()), Is.EqualTo(testPolicy1));
        }
        
        [Test]
        public void AsyncRequest_WhenAsyncRetryPolicyNull_UsesGetAsyncRetryPolicyFunc()
        {
            // arrange
            var testPolicy2 = Policy.HandleResult<RestResponse>(_ => true).RetryAsync();
            RetryConfiguration.GetAsyncRetryPolicyFunc = (Func<RequestOptions, AsyncPolicy<RestResponse>>)(_ => testPolicy2);
            
            // act
            new ApiFactory(new SdkConfiguration());
            
            // assert
            var asyncPolicy = ApiClient.GetAsyncPolicy<RestResponse>(new RequestOptions());
            Assert.That(asyncPolicy, Is.EqualTo(testPolicy2));
        }
    }
}
