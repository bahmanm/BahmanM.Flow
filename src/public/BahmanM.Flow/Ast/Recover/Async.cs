using BahmanM.Flow.Behaviour;

namespace BahmanM.Flow.Ast.Recover;

/// <summary>A recovery node that creates a new flow asynchronously after failure.</summary>
/// <param name="Source">The source flow whose failure may be recovered.</param>
/// <param name="Recover">An asynchronous function that creates the recovery flow from the failure.</param>
/// <typeparam name="T">The value type produced by the source and recovery flows.</typeparam>
internal sealed record Async<T> (IFlow<T> Source, Flow.Operations.Recover.Async<T> Recover) : INode<T>
{
    Task<Outcome<T>> INode<T>.Accept(IInterpreter interpreter) =>
        interpreter.Interpret(new Internal.Async<T>(Source, error => Recover(error)));

    IFlow<T> INode<T>.Apply(IBehaviourStrategy strategy) =>
        new Internal.Async<T>(Source, error => Recover(error)).Apply(strategy);
}
