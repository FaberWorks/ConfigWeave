using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace ConfigWeave;

internal sealed class SubstitutionConfigurationRoot(
    IConfigurationRoot inner,
    SubstitutionOptions? options = null) : IConfigurationRoot
{
    private readonly SubstitutionOptions _options = options ?? new SubstitutionOptions();

    public string? this[string key]
    {
        get
        {
            var raw = inner[key];
            return raw is null ? null : Resolve(raw, key);
        }
        set => inner[key] = value;
    }

    public IConfigurationSection GetSection(string key)
        => new SubstitutionConfigurationSection(inner.GetSection(key), this);

    public IEnumerable<IConfigurationSection> GetChildren()
        => inner.GetChildren().Select(s => new SubstitutionConfigurationSection(s, this));

    public IChangeToken GetReloadToken() => inner.GetReloadToken();
    public IEnumerable<IConfigurationProvider> Providers => inner.Providers;
    public void Reload() => inner.Reload();

    private string? Resolve(string value, string key)
    {
        if (!value.Contains("${"))
            return value;

        return new Resolver(inner, _options).Resolve(value, key);
    }
}