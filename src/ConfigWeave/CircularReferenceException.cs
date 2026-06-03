namespace ConfigWeave;

public class CircularReferenceException : ConfigurationResolutionException
{
    public string ReferencePath { get; }

    internal CircularReferenceException(IReadOnlyList<string> chain, string duplicate)
        : base($"Circular reference detected: {string.Join(" -> ", chain)} -> {duplicate}")
    {
        ReferencePath = string.Join(" -> ", chain) + " -> " + duplicate;
    }
}
