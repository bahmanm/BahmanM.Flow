using BahmanM.Flow.Description.Rendering.Projection;

namespace BahmanM.Flow.Description.Rendering.Abstractions;

internal interface IFormatter
{
    string Format(DescriptionView view);
}
