namespace BahmanM.Flow.Description.Rendering;

internal static class Renderer
{
    internal static string Describe<T>(IDescription<T> description)
    {
        IFormatter formatter = new PlainTextFormatter();
        return formatter.Format(description.Accept(new DescriptionViewProjector()));
    }
}
