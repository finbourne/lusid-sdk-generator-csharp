using System;
using System.Net;
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

        [Test]
        public void SyncRequest_UsesConfigurationFromSdkConfiguration_OnRateLimiting()
        {
            var statusCode = HttpStatusCode.TooManyRequests;
            var configuration = new SdkConfiguration
            {
                RateLimitRetries = 1
            };
            var expectedTimesCalled = configuration.RateLimitRetries + 1;
            AssertExpectedSyncPolicyBehaviour(statusCode, expectedTimesCalled, configuration);
        }

        [Test]
        public void SyncRequest_RequestOptionsOverrideSdkConfiguration_OnRateLimiting()
        {
            var statusCode = HttpStatusCode.TooManyRequests;
            var configuration = new SdkConfiguration
            {
                RateLimitRetries = 4
            };
            var requestOptions = new RequestOptions
            {
                RateLimitRetries = 1
            };
            var expectedTimesCalled = requestOptions.RateLimitRetries.Value + 1;
            AssertExpectedSyncPolicyBehaviour(statusCode, expectedTimesCalled, configuration, requestOptions);
        }

        private static void AssertExpectedSyncPolicyBehaviour(
            HttpStatusCode statusCode, 
            int expectedTimesCalled, 
            SdkConfiguration configuration = null,
            RequestOptions requestOptions = null)
        {
            // act
            new ApiFactory(configuration ?? new SdkConfiguration());
            Policy<ResponseBase> policy = ApiClient.GetSyncPolicy(requestOptions ?? new RequestOptions());
            
            // assert
            Assert.That(policy, Is.Not.Null);
            var timesCalled = 0;
            var result = policy.ExecuteAndCapture(() =>
            {
                timesCalled += 1;
                return new Response<string>
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
            var testPolicy = Policy.HandleResult<ResponseBase>(response => true).Retry();
            RetryConfiguration.RetryPolicy = testPolicy;
            
            // act
            new ApiFactory(new SdkConfiguration());

            // assert
            var policy = ApiClient.GetSyncPolicy(new RequestOptions());
            Assert.That(policy, Is.EqualTo(testPolicy));
        }
        
        [Test]
        public void SyncRequest_RetryPolicyOverridesGetRetryPolicyFunc()
        {
            // arrange
            var testPolicy1 = Policy.HandleResult<ResponseBase>(_ => true).Retry();
            var testPolicy2 = Policy.HandleResult<ResponseBase>(_ => true).Retry();

            // when both these properties are set, AsyncRetryPolicy should take precedence
            RetryConfiguration.RetryPolicy = testPolicy1;
            RetryConfiguration.GetRetryPolicyFunc = (Func<RequestOptions, Policy<ResponseBase>>)(_ => testPolicy2);

            // act
            new ApiFactory(new SdkConfiguration());
            
            // assert
            Assert.That(testPolicy1, Is.Not.EqualTo(testPolicy2));
            Assert.That(ApiClient.GetSyncPolicy(new RequestOptions()), Is.EqualTo(testPolicy1));
        }
        
        [Test]
        public void SyncRequest_WhenRetryPolicyNull_UsesGetRetryPolicyFunc()
        {
            // arrange
            var testPolicy2 = Policy.HandleResult<ResponseBase>(_ => true).Retry();
            RetryConfiguration.GetRetryPolicyFunc = (Func<RequestOptions, Policy<ResponseBase>>)(_ => testPolicy2);
            
            // act
            new ApiFactory(new SdkConfiguration());
            
            // assert
            var syncPolicy = ApiClient.GetSyncPolicy(new RequestOptions());
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
        
        [Test]
        public void AsyncRequest_UsesConfigurationFromSdkConfiguration_OnRateLimiting()
        {
            var statusCode = HttpStatusCode.TooManyRequests;
            var configuration = new SdkConfiguration
            {
                RateLimitRetries = 1
            };
            var expectedTimesCalled = configuration.RateLimitRetries + 1;
            AssertExpectedSyncPolicyBehaviour(statusCode, expectedTimesCalled, configuration);
        }

        [Test]
        public void AsyncRequest_RequestOptionsOverrideSdkConfiguration_OnRateLimiting()
        {
            var statusCode = HttpStatusCode.TooManyRequests;
            var configuration = new SdkConfiguration
            {
                RateLimitRetries = 4
            };
            var requestOptions = new RequestOptions
            {
                RateLimitRetries = 1
            };
            var expectedTimesCalled = requestOptions.RateLimitRetries.Value + 1;
            AssertExpectedSyncPolicyBehaviour(statusCode, expectedTimesCalled, configuration, requestOptions);
        }

        private static async System.Threading.Tasks.Task AssertExpectedAsyncPolicyBehaviour(
            HttpStatusCode statusCode, 
            int expectedTimesCalled,
            SdkConfiguration configuration = null,
            RequestOptions requestOptions = null)
        {
            // act
            new ApiFactory(configuration ?? new SdkConfiguration());
            AsyncPolicy<ResponseBase> policy = ApiClient.GetAsyncPolicy(requestOptions ?? new RequestOptions());
            
            // assert
            Assert.That(policy, Is.Not.Null);
            var timesCalled = 0;
            var result = await policy.ExecuteAsync(() =>
            {
                timesCalled += 1;
                return System.Threading.Tasks.Task.FromResult((ResponseBase)new Response<string>
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
            var testPolicy = Policy.HandleResult<ResponseBase>(response => true).RetryAsync();
            RetryConfiguration.AsyncRetryPolicy = testPolicy;
            
            // act
            new ApiFactory(new SdkConfiguration());

            // assert
            var policy = ApiClient.GetAsyncPolicy(new RequestOptions());
            Assert.That(policy, Is.EqualTo(testPolicy));
        }

        [Test]
        public void AsyncRequest_AsyncRetryPolicyOverridesGetAsyncRetryPolicyFunc()
        {
            // arrange
            var testPolicy1 = Policy.HandleResult<ResponseBase>(_ => true).RetryAsync();
            var testPolicy2 = Policy.HandleResult<ResponseBase>(_ => true).RetryAsync();

            // when both these properties are set, AsyncRetryPolicy should take precedence
            RetryConfiguration.AsyncRetryPolicy = testPolicy1;
            RetryConfiguration.GetAsyncRetryPolicyFunc = (Func<RequestOptions, AsyncPolicy<ResponseBase>>)(_ => testPolicy2);

            // act
            new ApiFactory(new SdkConfiguration());
            
            // assert
            Assert.That(testPolicy1, Is.Not.EqualTo(testPolicy2));
            Assert.That(ApiClient.GetAsyncPolicy(new RequestOptions()), Is.EqualTo(testPolicy1));
        }
        
        [Test]
        public void AsyncRequest_WhenAsyncRetryPolicyNull_UsesGetAsyncRetryPolicyFunc()
        {
            // arrange
            var testPolicy2 = Policy.HandleResult<ResponseBase>(_ => true).RetryAsync();
            RetryConfiguration.GetAsyncRetryPolicyFunc = (Func<RequestOptions, AsyncPolicy<ResponseBase>>)(_ => testPolicy2);
            
            // act
            new ApiFactory(new SdkConfiguration());
            
            // assert
            var asyncPolicy = ApiClient.GetAsyncPolicy(new RequestOptions());
            Assert.That(asyncPolicy, Is.EqualTo(testPolicy2));
        }
    }
}
