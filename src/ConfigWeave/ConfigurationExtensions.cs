using Microsoft.Extensions.Configuration;

namespace ConfigWeave;

public static class ConfigurationExtensions
{
    public static IConfigurationRoot WithSubstitution(
        this IConfigurationRoot configuration,
        SubstitutionOptions? options = null)
        => new SubstitutionConfigurationRoot(configuration, options);

    public static IConfigurationBuilder AddSubstitution(
        this IConfigurationBuilder builder,
        SubstitutionOptions? options = null)
    {
        var sources = builder.Sources.ToList();
        builder.Sources.Clear();

        var innerBuilder = new ConfigurationBuilder();
        foreach (var (key, value) in builder.Properties)
            innerBuilder.Properties[key] = value;
        foreach (var source in sources)
            innerBuilder.Add(source);

        return builder.AddConfiguration(innerBuilder.Build().WithSubstitution(options));
    }
}