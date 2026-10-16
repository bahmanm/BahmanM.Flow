using BahmanM.Flow.Execution.Resource.Support;

namespace BahmanM.Flow.Execution.Resource;

internal class WithResourceAsyncExecutor(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<T>> Interpret<TResource, T>(Ast.Resource.WithResourceAsync<TResource, T> node)
        where TResource : IAsyncDisposable
    {
        TResource resource;
        try
        {
            resource = await node.AcquireAsync().ConfigureAwait(false);
        }
        catch (Exception acquireEx)
        {
            return Outcome.Failure<T>(acquireEx);
        }

        return await ResourceAsyncScope.ExecuteAsync(interpreter, options, resource, node.Use).ConfigureAwait(false);
    }
}
