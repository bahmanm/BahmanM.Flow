namespace BahmanM.Flow.Behaviour;

/// <summary>
/// The contract for implementing a custom, cross-cutting behaviour that can be applied to a Flow.
/// </summary>
/// <remarks>
/// Behaviours are applied using the <see cref="FlowExtensions.WithBehaviour{T}(IFlow{T}, IBehaviour)"/> operator
/// and allow for the introduction of stateful policies like circuit breakers, complex logging, or caching.
/// </remarks>
public interface IBehaviour
{
    /// <summary>
    /// A string identifying this behaviour in description inspection output.
    /// </summary>
    string OperationType { get; }

    /// <summary>
    /// Applies the behaviour's logic to a given <see cref="IFlow{T}"/>.
    /// </summary>
    /// <param name="originalFlow">The original <see cref="IFlow{T}"/> to be decorated.</param>
    /// <returns>A new <see cref="IFlow{T}"/> that includes the behaviour's logic.</returns>
    /// <typeparam name="T">The type of the value in the Flow.</typeparam>
    IFlow<T> Apply<T>(IFlow<T> originalFlow);
}
