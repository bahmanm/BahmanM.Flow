namespace BahmanM.Flow.Description.Serialisation;

internal sealed record DescriptionView(
    string OperatorName,
    string Id,
    IReadOnlyList<Type> TypeArguments,
    IReadOnlyList<DescriptionField> Fields,
    DescriptionView? Upstream);

internal sealed record DescriptionField(string Label, string Value);
