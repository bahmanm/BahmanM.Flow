namespace BahmanM.Flow.Description;

internal interface IDescription
{
    string Id { get; }
    TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor);
}
