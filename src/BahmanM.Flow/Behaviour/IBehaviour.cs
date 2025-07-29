namespace BahmanM.Flow.Behaviour;

public interface IBehaviour
{
    string OperationType { get; }
    IFlow<T> Apply<T>(IFlow<T> originalFlow);
}