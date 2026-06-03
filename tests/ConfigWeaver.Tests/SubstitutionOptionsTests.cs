using ConfigWeave;
using Microsoft.Extensions.Configuration;

namespace ConfigWeave.Tests;

public class SubstitutionOptionsTests
{
    private static IConfigurationRoot BuildConfig(
        Dictionary<string, string?> values,
        SubstitutionOptions? options = null)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build()
            .WithSubstitution(options);

    // --- UnresolvedKeyBehavior.Throw (default) ---

    [Fact]
    public void MissingKey_DefaultBehavior_Throws()
    {
        var config = BuildConfig(new() { ["Key"] = "${Missing}" });
        Assert.Throws<ConfigurationResolutionException>(() => config["Key"]);
    }

    // --- UnresolvedKeyBehavior.ReturnNull ---

    [Fact]
    public void MissingKey_ReturnNull_ReturnsNull()
    {
        var config = BuildConfig(
            new() { ["Key"] = "${Missing}" },
            new SubstitutionOptions { UnresolvedKeyBehavior = UnresolvedKeyBehavior.ReturnNull });

        Assert.Null(config["Key"]);
    }

    [Fact]
    public void MissingKeyInInterpolation_ReturnNull_ReturnsNull()
    {
        var config = BuildConfig(
            new() { ["Key"] = "prefix-${Missing}-suffix" },
            new SubstitutionOptions { UnresolvedKeyBehavior = UnresolvedKeyBehavior.ReturnNull });

        Assert.Null(config["Key"]);
    }

    [Fact]
    public void MissingEnvVar_ReturnNull_ReturnsNull()
    {
        Environment.SetEnvironmentVariable("CONFIGWEAVE_TEST_MISSING", null);
        var config = BuildConfig(
            new() { ["Key"] = "${@CONFIGWEAVE_TEST_MISSING}" },
            new SubstitutionOptions { UnresolvedKeyBehavior = UnresolvedKeyBehavior.ReturnNull });

        Assert.Null(config["Key"]);
    }

    // --- UnresolvedKeyBehavior.KeepPattern ---

    [Fact]
    public void MissingKey_KeepPattern_ReturnsPattern()
    {
        var config = BuildConfig(
            new() { ["Key"] = "${Missing}" },
            new SubstitutionOptions { UnresolvedKeyBehavior = UnresolvedKeyBehavior.KeepPattern });

        Assert.Equal("${Missing}", config["Key"]);
    }

    [Fact]
    public void MissingKeyInInterpolation_KeepPattern_PreservesLiterals()
    {
        var config = BuildConfig(
            new() { ["Key"] = "prefix-${Missing}-suffix" },
            new SubstitutionOptions { UnresolvedKeyBehavior = UnresolvedKeyBehavior.KeepPattern });

        Assert.Equal("prefix-${Missing}-suffix", config["Key"]);
    }

    [Fact]
    public void MissingEnvVar_KeepPattern_ReturnsPattern()
    {
        Environment.SetEnvironmentVariable("CONFIGWEAVE_TEST_MISSING", null);
        var config = BuildConfig(
            new() { ["Key"] = "${@CONFIGWEAVE_TEST_MISSING}" },
            new SubstitutionOptions { UnresolvedKeyBehavior = UnresolvedKeyBehavior.KeepPattern });

        Assert.Equal("${@CONFIGWEAVE_TEST_MISSING}", config["Key"]);
    }

    // --- Default value takes precedence over UnresolvedKeyBehavior ---

    [Fact]
    public void MissingKeyWithDefault_ReturnNullBehavior_ReturnsDefault()
    {
        var config = BuildConfig(
            new() { ["Key"] = "${Missing|fallback}" },
            new SubstitutionOptions { UnresolvedKeyBehavior = UnresolvedKeyBehavior.ReturnNull });

        Assert.Equal("fallback", config["Key"]);
    }

    // --- AddSubstitution builder extension ---

    [Fact]
    public void AddSubstitution_ResolvesReferences()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Host"] = "localhost",
                ["ConnectionString"] = "Server=${Host}"
            })
            .AddSubstitution()
            .Build();

        Assert.Equal("Server=localhost", config["ConnectionString"]);
    }

    [Fact]
    public void AddSubstitution_WithOptions_RespectsUnresolvedBehavior()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Key"] = "${Missing}"
            })
            .AddSubstitution(new SubstitutionOptions { UnresolvedKeyBehavior = UnresolvedKeyBehavior.KeepPattern })
            .Build();

        Assert.Equal("${Missing}", config["Key"]);
    }
}