using ConfigWeave;
using Microsoft.Extensions.Configuration;

namespace ConfigWeave.Tests;

public class WithSubstitutionTests
{
    private static IConfigurationRoot BuildConfig(Dictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build()
            .WithSubstitution();

    [Fact]
    public void PlainString_IsReturnedUnchanged()
    {
        var config = BuildConfig(new() { ["Key"] = "hello" });
        Assert.Equal("hello", config["Key"]);
    }

    [Fact]
    public void MissingKey_ReturnsNull()
    {
        var config = BuildConfig(new() { ["Key"] = "hello" });
        Assert.Null(config["NoSuchKey"]);
    }

    [Fact]
    public void NullValue_ReturnsNull()
    {
        var config = BuildConfig(new() { ["Key"] = null });
        Assert.Null(config["Key"]);
    }

    [Fact]
    public void EmptyString_IsReturnedUnchanged()
    {
        var config = BuildConfig(new() { ["Key"] = "" });
        Assert.Equal("", config["Key"]);
    }

    [Fact]
    public void NestedKey_IsReturnedUnchanged()
    {
        var config = BuildConfig(new() { ["Section:Key"] = "value" });
        Assert.Equal("value", config["Section:Key"]);
    }

    [Fact]
    public void MultipleKeys_AreReturnedUnchanged()
    {
        var config = BuildConfig(new()
        {
            ["Key1"] = "value1",
            ["Key2"] = "value2",
            ["Key3"] = "value3"
        });

        Assert.Equal("value1", config["Key1"]);
        Assert.Equal("value2", config["Key2"]);
        Assert.Equal("value3", config["Key3"]);
    }

    [Fact]
    public void ValueWithDollarButNoSubstitutionSyntax_IsReturnedUnchanged()
    {
        var config = BuildConfig(new() { ["Key"] = "costs $10" });
        Assert.Equal("costs $10", config["Key"]);
    }
}
