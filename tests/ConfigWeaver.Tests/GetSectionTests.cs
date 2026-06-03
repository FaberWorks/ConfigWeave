using ConfigWeave;
using Microsoft.Extensions.Configuration;

namespace ConfigWeave.Tests;

public class GetSectionTests
{
    private static IConfigurationRoot BuildConfig(Dictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build()
            .WithSubstitution();

    [Fact]
    public void GetSection_IndexerResolvesSubstitution()
    {
        var config = BuildConfig(new()
        {
            ["Database:Host"] = "localhost",
            ["Database:ConnectionString"] = "Server=${Database:Host}"
        });

        var section = config.GetSection("Database");
        Assert.Equal("Server=localhost", section["ConnectionString"]);
    }

    [Fact]
    public void GetSection_ValueResolvesSubstitution()
    {
        var config = BuildConfig(new()
        {
            ["Host"] = "localhost",
            ["ResolvedHost"] = "${Host}"
        });

        var section = config.GetSection("ResolvedHost");
        Assert.Equal("localhost", section.Value);
    }

    [Fact]
    public void GetSection_NestedGetSectionResolvesSubstitution()
    {
        var config = BuildConfig(new()
        {
            ["Database:Primary:Host"] = "localhost",
            ["Database:Primary:ConnectionString"] = "Server=${Database:Primary:Host}"
        });

        var section = config.GetSection("Database").GetSection("Primary");
        Assert.Equal("Server=localhost", section["ConnectionString"]);
    }

    [Fact]
    public void GetChildren_ValuesAreResolved()
    {
        var config = BuildConfig(new()
        {
            ["App:Name"] = "MyApp",
            ["App:Title"] = "${App:Name} Service"
        });

        var children = config.GetSection("App").GetChildren().ToDictionary(s => s.Key, s => s.Value);
        Assert.Equal("MyApp", children["Name"]);
        Assert.Equal("MyApp Service", children["Title"]);
    }
}
