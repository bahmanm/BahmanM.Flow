namespace BahmanM.Flow.Description.Formatting;

internal static class DescriptionFormatting
{
    internal static string Describe<T>(IDescription<T> description) =>
        new PlainTextFormatter().Format(description.Accept(new DescriptionViewProjector()));
}
