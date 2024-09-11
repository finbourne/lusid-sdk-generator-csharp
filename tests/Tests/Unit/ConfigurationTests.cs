using System;
using NUnit.Framework;
using SdkConfiguration = TO_BE_REPLACED_PROJECT_NAME.Client.Configuration;

namespace Finbourne.Sdk.Extensions.Tests.Unit;

public class ConfigurationTests
{
    [Test]
    public void WhenInvalidTimeoutSet_ThrowsException()
    {
        // arrange
        var configuration = new SdkConfiguration();
        
        // act
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            configuration.TimeoutMs = 0;
        });
        
        // assert
        Assert.That(exception.Message, Is.EqualTo("TimeoutMs must be a positive integer between 1 and 2147483647"));
    }

    [Test]
    public void WhenInvalidRateLimitRetriesSet_ThrowsException()
    {
        // arrange
        var configuration = new SdkConfiguration();
        
        // act
        var exception = Assert.Throws<ArgumentException>(() => configuration.RateLimitRetries = -1);
        
        // assert
        Assert.That(exception.Message, Is.EqualTo("RateLimitRetries must be a positive integer between 0 and 2147483647"));
    }

    [Test]
    public void WhenOldTimeoutPropertySet_TimeoutMsIsUpdated()
    {
        // arrange
        var configuration = new SdkConfiguration();

        // act
        configuration.Timeout = 21_000;

        // assert
        Assert.That(configuration.Timeout, Is.EqualTo(21_000));
        Assert.That(configuration.TimeoutMs, Is.EqualTo(21_000));
    }
    
    [Test]
    public void WhenTimeoutMsSet_TimeoutIsUpdated()
    {
        // arrange
        var configuration = new SdkConfiguration();

        // act
        configuration.TimeoutMs = 22_000;

        // assert
        Assert.That(configuration.Timeout, Is.EqualTo(22_000));
        Assert.That(configuration.TimeoutMs, Is.EqualTo(22_000));
    }
}