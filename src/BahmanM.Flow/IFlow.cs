namespace BahmanM.Flow;

public interface IFlow<T>
{
}

#region Internal Contracts

internal interface IVisitableFlow<T> : IFlow<T>
{
    Task<Outcome<T>> ExecuteWith(FlowEngine engine);
}

#endregion