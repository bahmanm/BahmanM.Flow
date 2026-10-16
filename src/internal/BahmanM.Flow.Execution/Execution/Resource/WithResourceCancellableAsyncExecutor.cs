using BahmanM.Flow.Execution.Resource.Support;

namespace BahmanM.Flow.Execution.Resource;

internal class WithResourceCancellableAsyncExecutor(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<T>> Interpret<TResource, T>(Ast.Resource.WithResourceCancellableAsync<TResource, T> node)
        where TResource : IAsyncDisposable
    {
        TResource resource;
        try
        {
            resource = await node.AcquireAsync(options.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception acquireEx)
        {
            return Outcome.Failure<T>(acquireEx);
        }

        return await ResourceAsyncScope.ExecuteAsync(interpreter, options, resource, node.Use).ConfigureAwait(false);
    }
}
