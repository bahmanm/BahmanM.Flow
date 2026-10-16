using BahmanM.Flow.Description.Rendering.Abstractions;
using BahmanM.Flow.Description.Rendering.Formatters;
using BahmanM.Flow.Description.Rendering.Projection;

namespace BahmanM.Flow.Description.Rendering;

internal static class Renderer
{
    internal static string Describe<T>(IDescription<T> description)
    {
        IFormatter formatter = new PlainTextFormatter();
        return formatter.Format(description.Accept(new DescriptionViewProjector()));
    }
}
