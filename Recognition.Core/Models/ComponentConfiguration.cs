namespace Recognition.Core;

public sealed class ComponentConfiguration
{
    public string ComponentId { get; set; } = string.Empty;

    public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public ComponentConfiguration Clone()
    {
        return new ComponentConfiguration
        {
            ComponentId = ComponentId,
            Parameters = new Dictionary<string, string>(Parameters, StringComparer.OrdinalIgnoreCase)
        };
    }
}
