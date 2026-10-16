namespace BahmanM.Flow;

/// <summary>
/// A declarative recipe for a sequence of operations that results in a value of
/// type <typeparamref name="T"/> or an <see cref="Exception"/>.
/// </summary>
/// <remarks>
/// Flow factories and operators compose description nodes for the computation. When an
/// <see cref="IFlow{T}"/> is passed to <c>FlowEngine</c>, its description is lowered to
/// an Abstract Syntax Tree (AST) and interpreted. Callbacks may capture mutable state, so the
/// description's structure alone does not guarantee that callback state is safe to share.
/// </remarks>
/// <typeparam name="T">The type of the value that will be produced by the successful execution of the Flow.</typeparam>
public interface IFlow<T>
{
}
