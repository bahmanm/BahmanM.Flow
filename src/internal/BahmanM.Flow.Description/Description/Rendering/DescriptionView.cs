namespace BahmanM.Flow.Description.Rendering;

internal sealed record DescriptionView(
    string OperatorName,
    string Id,
    IReadOnlyList<Type> TypeArguments,
    IReadOnlyList<DescriptionField> Fields,
    IReadOnlyList<DescriptionChild> Children);

internal sealed record DescriptionField(string Label, string Value);

internal sealed record DescriptionChild(string Label, DescriptionView View);
