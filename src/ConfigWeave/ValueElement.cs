namespace ConfigWeave;

internal abstract record ValueElement
{
    internal sealed record Literal(string Text) : ValueElement;
    internal sealed record Reference(string Key, string? Default) : ValueElement;
    internal sealed record EnvironmentVariable(string Name, string? Default) : ValueElement;
}
