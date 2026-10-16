namespace BahmanM.Flow.Description;

internal interface IDescription<T> : IDescription, IFlow<T>
{
    Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory);
}

internal abstract record Node<T> : IDescription<T>
{
    protected Node() => Id = Guid.NewGuid().ToString("N");

    public string Id { get; }
    public abstract TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor);
    public abstract Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory);
}
