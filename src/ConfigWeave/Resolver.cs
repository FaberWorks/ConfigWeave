using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace ConfigWeave;

internal sealed class Resolver(IConfigurationRoot config, SubstitutionOptions options)
{
    public string? Resolve(string value, string key)
        => InternalResolve(value, key, ImmutableList<string>.Empty);

    private string? InternalResolve(string value, string key, ImmutableList<string> visitedKeys)
    {
        if (visitedKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
            throw new CircularReferenceException(visitedKeys, key);

        return ResolveValue(value, visitedKeys.Add(key));
    }

    private string? ResolveValue(string value, ImmutableList<string> visitedKeys)
    {
        var elements = ValueParser.Parse(value);

        if (elements.Count == 1 && elements[0] is ValueElement.Literal literal)
            return literal.Text;

        var sb = new StringBuilder();
        foreach (var element in elements)
        {
            var part = element switch
            {
                ValueElement.Literal l             => l.Text,
                ValueElement.Reference r           => ResolveConfigReference(r.Key, r.Default, visitedKeys),
                ValueElement.EnvironmentVariable e => ResolveEnvironmentVariable(e.Name, e.Default),
                _                                  => throw new UnreachableException()
            };

            if (part is null) return null;
            sb.Append(part);
        }
        return sb.ToString();
    }

    private string? ResolveConfigReference(string key, string? defaultValue, ImmutableList<string> visitedKeys)
    {
        var raw = config[key];
        if (raw is null)
        {
            if (defaultValue is not null) return defaultValue;
            return options.UnresolvedKeyBehavior switch
            {
                UnresolvedKeyBehavior.Throw       => throw new ConfigurationResolutionException($"Referenced key '{key}' not found."),
                UnresolvedKeyBehavior.ReturnNull   => null,
                UnresolvedKeyBehavior.KeepPattern  => $"${{{key}}}",
                _                                  => throw new UnreachableException()
            };
        }

        return InternalResolve(raw, key, visitedKeys);
    }

    private string? ResolveEnvironmentVariable(string name, string? defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (value is null)
        {
            if (defaultValue is not null) return defaultValue;
            return options.UnresolvedKeyBehavior switch
            {
                UnresolvedKeyBehavior.Throw       => throw new ConfigurationResolutionException($"Environment variable '{name}' is not set."),
                UnresolvedKeyBehavior.ReturnNull   => null,
                UnresolvedKeyBehavior.KeepPattern  => $"${{@{name}}}",
                _                                  => throw new UnreachableException()
            };
        }

        return value;
    }
}