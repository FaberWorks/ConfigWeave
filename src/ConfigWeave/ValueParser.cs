using System.Text;

namespace ConfigWeave;

internal static class ValueParser
{
    public static IReadOnlyList<ValueElement> Parse(string value)
    {
        var elements = new List<ValueElement>();
        var literal = new StringBuilder();
        var i = 0;

        while (i < value.Length)
        {
            if (value[i] != '$')
            {
                literal.Append(value[i++]);
                continue;
            }

            // '$' found — peek at next character
            if (i + 1 >= value.Length)
            {
                // Trailing '$' with nothing after it — literal
                literal.Append(value[i++]);
                continue;
            }

            if (value[i + 1] == '$')
            {
                // '$$' escape sequence — emit a single '$'
                literal.Append('$');
                i += 2;
                continue;
            }

            if (value[i + 1] != '{')
            {
                // '$' not followed by '{' — literal
                literal.Append(value[i++]);
                continue;
            }

            // '${' — look for the closing '}'
            var close = value.IndexOf('}', i + 2);
            if (close == -1)
                throw new ConfigurationParseException($"Unclosed '${{' in value: {value}");

            // Flush accumulated literal text before the reference
            if (literal.Length > 0)
            {
                elements.Add(new ValueElement.Literal(literal.ToString()));
                literal.Clear();
            }

            var content = value[(i + 2)..close];
            elements.Add(ParseReference(content));
            i = close + 1;
        }

        if (literal.Length > 0)
            elements.Add(new ValueElement.Literal(literal.ToString()));

        return elements;
    }

    private static ValueElement ParseReference(string content)
    {
        var pipeIndex = content.IndexOf('|');
        var key = pipeIndex == -1 ? content : content[..pipeIndex];
        var defaultValue = pipeIndex == -1 ? null : content[(pipeIndex + 1)..];

        if (key.StartsWith('@'))
            return new ValueElement.EnvironmentVariable(key[1..], defaultValue);

        return new ValueElement.Reference(key, defaultValue);
    }
}
