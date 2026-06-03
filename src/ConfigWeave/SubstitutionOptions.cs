namespace ConfigWeave;

public sealed class SubstitutionOptions
{
    /// <summary>
    /// Behavior when a referenced config key or environment variable is not found and no default value is provided.
    /// Default: <see cref="UnresolvedKeyBehavior.Throw"/>.
    /// </summary>
    public UnresolvedKeyBehavior UnresolvedKeyBehavior { get; init; } = UnresolvedKeyBehavior.Throw;
}

public enum UnresolvedKeyBehavior
{
    /// <summary>Throw a <see cref="ConfigurationResolutionException"/>.</summary>
    Throw,

    /// <summary>Return null for the entire value containing the unresolved reference.</summary>
    ReturnNull,

    /// <summary>Leave the original <c>${Key}</c> / <c>${@ENV_VAR}</c> pattern in the resolved string.</summary>
}