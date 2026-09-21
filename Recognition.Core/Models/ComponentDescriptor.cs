namespace Recognition.Core;

public sealed record ComponentDescriptor(
    string Id,
    string DisplayName,
    string Description,
    IReadOnlyList<ParameterDefinition> Parameters);
