namespace Recognition.Core;

public sealed record ParameterDefinition(
    string Key,
    string DisplayName,
    ParameterValueKind ValueKind,
    string DefaultValue,
    string? Description = null,
    IReadOnlyList<ParameterOption>? Options = null,
    double? Minimum = null,
    double? Maximum = null,
    double? Step = null);
