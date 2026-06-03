using ConfigWeave;
using Microsoft.Extensions.Configuration;

namespace ConfigWeave.Tests;

public class ResolverTests
{
    private static IConfigurationRoot BuildConfig(Dictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build()
            .WithSubstitution();

    // --- Basic substitution ---

    [Fact]
    public void SimpleReference_ResolvesValue()
    {
        var config = BuildConfig(new()
        {
            ["Host"] = "localhost",
            ["ConnectionString"] = "Server=${Host}"
        });
        Assert.Equal("Server=localhost", config["ConnectionString"]);
    }

    [Fact]
    public void NestedPathReference_ResolvesValue()
    {
        var config = BuildConfig(new()
        {
            ["Database:Host"] = "localhost",
            ["Database:ConnectionString"] = "Server=${Database:Host}"
        });
        Assert.Equal("Server=localhost", config["Database:ConnectionString"]);
    }

    [Fact]
    public void MultipleReferencesInOneValue_AllResolved()
    {
        var config = BuildConfig(new()
        {
            ["Database:Host"] = "localhost",
            ["Database:Port"] = "5432",
            ["Database:ConnectionString"] = "Server=${Database:Host};Port=${Database:Port}"
        });
        Assert.Equal("Server=localhost;Port=5432", config["Database:ConnectionString"]);
    }

    // --- Recursive resolution ---

    [Fact]
    public void ChainedReferences_ResolvesRecursively()
    {
        var config = BuildConfig(new()
        {
            ["App:Name"] = "MyApp",
            ["App:Version"] = "1.0.0",
            ["App:FullName"] = "${App:Name} v${App:Version}",
            ["App:Description"] = "Welcome to ${App:FullName}!"
        });
        Assert.Equal("Welcome to MyApp v1.0.0!", config["App:Description"]);
    }

    // --- Default values ---

    [Fact]
    public void MissingKeyWithDefault_ReturnsDefault()
    {
        var config = BuildConfig(new()
        {
            ["Port"] = "${MISSING|8080}"
        });
        Assert.Equal("8080", config["Port"]);
    }

    [Fact]
    public void ExistingKeyWithDefault_ReturnsResolvedValue()
    {
        var config = BuildConfig(new()
        {
            ["ActualPort"] = "9090",
            ["Port"] = "${ActualPort|8080}"
        });
        Assert.Equal("9090", config["Port"]);
    }

    [Fact]
    public void MissingKeyWithoutDefault_Throws()
    {
        var config = BuildConfig(new()
        {
            ["Key"] = "${MISSING}"
        });
        Assert.Throws<ConfigurationResolutionException>(() => config["Key"]);
    }

    // --- Environment variables ---

    [Fact]
    public void EnvironmentVariable_ResolvesFromEnvironment()
    {
        Environment.SetEnvironmentVariable("LIBCONFIG_TEST_HOST", "prod.server.com");
        try
        {
            var config = BuildConfig(new() { ["Host"] = "${@LIBCONFIG_TEST_HOST}" });
            Assert.Equal("prod.server.com", config["Host"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("LIBCONFIG_TEST_HOST", null);
        }
    }

    [Fact]
    public void MissingEnvironmentVariableWithDefault_ReturnsDefault()
    {
        Environment.SetEnvironmentVariable("LIBCONFIG_TEST_MISSING", null);
        var config = BuildConfig(new() { ["Host"] = "${@LIBCONFIG_TEST_MISSING|localhost}" });
        Assert.Equal("localhost", config["Host"]);
    }

    [Fact]
    public void MissingEnvironmentVariableWithoutDefault_Throws()
    {
        Environment.SetEnvironmentVariable("LIBCONFIG_TEST_MISSING", null);
        var config = BuildConfig(new() { ["Host"] = "${@LIBCONFIG_TEST_MISSING}" });
        Assert.Throws<ConfigurationResolutionException>(() => config["Host"]);
    }

    [Fact]
    public void EnvironmentVariable_IsReadFreshOnEachAccess()
    {
        Environment.SetEnvironmentVariable("LIBCONFIG_TEST_DYNAMIC", "first");
        try
        {
            var config = BuildConfig(new() { ["Key"] = "${@LIBCONFIG_TEST_DYNAMIC}" });
            Assert.Equal("first", config["Key"]);

            Environment.SetEnvironmentVariable("LIBCONFIG_TEST_DYNAMIC", "second");
            Assert.Equal("second", config["Key"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("LIBCONFIG_TEST_DYNAMIC", null);
        }
    }

    // --- Circular references ---

    [Fact]
    public void DirectCircularReference_Throws()
    {
        var config = BuildConfig(new()
        {
            ["Key1"] = "${Key2}",
            ["Key2"] = "${Key1}"
        });
        Assert.Throws<CircularReferenceException>(() => config["Key1"]);
    }

    [Fact]
    public void IndirectCircularReference_Throws()
    {
        var config = BuildConfig(new()
        {
            ["A"] = "${B}",
            ["B"] = "${C}",
            ["C"] = "${A}"
        });
        Assert.Throws<CircularReferenceException>(() => config["A"]);
    }

    [Fact]
    public void CircularReferenceException_ContainsPath()
    {
        var config = BuildConfig(new()
        {
            ["Key1"] = "${Key2}",
            ["Key2"] = "${Key1}"
        });
        var ex = Assert.Throws<CircularReferenceException>(() => config["Key1"]);
        Assert.Contains("Key1", ex.ReferencePath);
        Assert.Contains("Key2", ex.ReferencePath);
    }
    
    [Fact]
    public void NestedSubstitution_Resolves()
    {
        var config = BuildConfig(new()
        {
            ["Database:ConnectionString"] = "Server=${Host};Port=${Port}",
            ["Host"] = "localhost",
            ["Port"] = "5432"
        });
        Assert.Equal("Server=localhost;Port=5432", config["Database:ConnectionString"]);
    }
}
