using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace ConfigWeave;

internal sealed class SubstitutionConfigurationSection : IConfigurationSection
{
    private readonly IConfigurationSection _inner;
    private readonly SubstitutionConfigurationRoot _root;

    public SubstitutionConfigurationSection(IConfigurationSection inner, SubstitutionConfigurationRoot root)
    {
        _inner = inner;
        _root = root;
    }

    public string Key => _inner.Key;
    public string Path => _inner.Path;

    public string? Value
    {
        get => _root[_inner.Path];
        set => _inner.Value = value;
    }

    public string? this[string key]
    {
        get => _root[$"{_inner.Path}:{key}"];
        set => _inner[key] = value;
    }

    public IConfigurationSection GetSection(string key)
        => new SubstitutionConfigurationSection(_inner.GetSection(key), _root);

    public IEnumerable<IConfigurationSection> GetChildren()
        => _inner.GetChildren().Select(s => new SubstitutionConfigurationSection(s, _root));

    public IChangeToken GetReloadToken() => _inner.GetReloadToken();
}
