using System;
using NUnit.Framework;

namespace Finbourne.Sdk.Extensions.Tests.Unit;

public class ConfigurationOptionsTests
{
    [Test]
    public void WhenInvalidTimeoutSet_ThrowsException()
    {
        // arrange
        var configurationOptions = new ConfigurationOptions();
        
        // act
        var exception = Assert.Throws<ArgumentException>(() => configurationOptions.TimeoutMs = 0);
        
        // assert
        Assert.That(exception.Message, Is.EqualTo("TimeoutMs must be a positive integer between 1 and 2147483647"));
    }

    [Test]
    public void WhenInvalidRateLimitRetriesSet_ThrowsException()
    {
        // arrange
        var configurationOptions = new ConfigurationOptions();
        
        // act
        var exception = Assert.Throws<ArgumentException>(() => configurationOptions.RateLimitRetries = -1);
        
        // assert
        Assert.That(exception.Message, Is.EqualTo("RateLimitRetries must be a positive integer between 0 and 2147483647"));
    }
}