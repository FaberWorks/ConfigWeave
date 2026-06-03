using ConfigWeave;

namespace ConfigWeave.Tests;

public class ValueParserTests
{
    // --- Plain text ---

    [Fact]
    public void PlainText_ReturnsSingleLiteral()
    {
        var result = ValueParser.Parse("hello");
        var element = Assert.Single(result);
        Assert.Equal(new ValueElement.Literal("hello"), element);
    }

    [Fact]
    public void EmptyString_ReturnsEmpty()
    {
        Assert.Empty(ValueParser.Parse(""));
    }

    // --- Config references ---

    [Fact]
    public void SimpleReference_ReturnsSingleReference()
    {
        var result = ValueParser.Parse("${Key}");
        var element = Assert.Single(result);
        Assert.Equal(new ValueElement.Reference("Key", null), element);
    }

    [Fact]
    public void NestedPathReference_PreservesColonSeparator()
    {
        var result = ValueParser.Parse("${Section:Key}");
        var element = Assert.Single(result);
        Assert.Equal(new ValueElement.Reference("Section:Key", null), element);
    }

    [Fact]
    public void ReferenceWithDefault_ParsesKeyAndDefault()
    {
        var result = ValueParser.Parse("${Key|fallback}");
        var element = Assert.Single(result);
        Assert.Equal(new ValueElement.Reference("Key", "fallback"), element);
    }

    [Fact]
    public void ReferenceWithEmptyDefault_ParsesEmptyStringDefault()
    {
        var result = ValueParser.Parse("${Key|}");
        var element = Assert.Single(result);
        Assert.Equal(new ValueElement.Reference("Key", ""), element);
    }

    // --- Environment variables ---

    [Fact]
    public void EnvironmentVariable_ReturnsSingleEnvironmentVariable()
    {
        var result = ValueParser.Parse("${@MY_VAR}");
        var element = Assert.Single(result);
        Assert.Equal(new ValueElement.EnvironmentVariable("MY_VAR", null), element);
    }

    [Fact]
    public void EnvironmentVariableWithDefault_ParsesNameAndDefault()
    {
        var result = ValueParser.Parse("${@MY_VAR|localhost}");
        var element = Assert.Single(result);
        Assert.Equal(new ValueElement.EnvironmentVariable("MY_VAR", "localhost"), element);
    }

    // --- Interpolation ---

    [Fact]
    public void InterpolatedValue_ParsesLiteralsAndReferences()
    {
        var result = ValueParser.Parse("Server=${Database:Host};Port=${Database:Port}");

        Assert.Equal(4, result.Count);
        Assert.Equal(new ValueElement.Literal("Server="), result[0]);
        Assert.Equal(new ValueElement.Reference("Database:Host", null), result[1]);
        Assert.Equal(new ValueElement.Literal(";Port="), result[2]);
        Assert.Equal(new ValueElement.Reference("Database:Port", null), result[3]);
    }

    [Fact]
    public void MixedEnvAndConfigReferences_ParsesCorrectly()
    {
        var result = ValueParser.Parse("${@HOST|localhost}:${Port}");

        Assert.Equal(3, result.Count);
        Assert.Equal(new ValueElement.EnvironmentVariable("HOST", "localhost"), result[0]);
        Assert.Equal(new ValueElement.Literal(":"), result[1]);
        Assert.Equal(new ValueElement.Reference("Port", null), result[2]);
    }

    // --- Escaping ---

    [Fact]
    public void DoubleDollar_EmitsLiteralDollarSign()
    {
        var result = ValueParser.Parse("$$");
        var element = Assert.Single(result);
        Assert.Equal(new ValueElement.Literal("$"), element);
    }

    [Fact]
    public void EscapedReference_EmitsLiteralBraces()
    {
        // "$${Key}" should produce literal "${Key}"
        var result = ValueParser.Parse("$${Key}");
        var element = Assert.Single(result);
        Assert.Equal(new ValueElement.Literal("${Key}"), element);
    }

    [Fact]
    public void DollarSignInText_IsLiteral()
    {
        var result = ValueParser.Parse("costs $10");
        var element = Assert.Single(result);
        Assert.Equal(new ValueElement.Literal("costs $10"), element);
    }

    // --- Malformed syntax ---

    [Fact]
    public void UnclosedReference_Throws()
    {
        Assert.Throws<ConfigurationParseException>(() => ValueParser.Parse("${Key"));
    }

    [Fact]
    public void TrailingDollar_IsLiteral()
    {
        var result = ValueParser.Parse("value$");
        var element = Assert.Single(result);
        Assert.Equal(new ValueElement.Literal("value$"), element);
    }
}
