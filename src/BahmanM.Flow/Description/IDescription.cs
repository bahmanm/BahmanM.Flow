namespace BahmanM.Flow.Description;

internal interface IDescription
{
    string Id { get; }
    string ShortId { get; }
    string OperatorName { get; }
    string OperatorDisplayName { get; }
    Type ValueType { get; }
    IDescription? Source { get; }
}
