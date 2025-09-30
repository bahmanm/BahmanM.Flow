using BahmanM.Flow.Support;

namespace BahmanM.Flow.Execution.Resource.Support;

internal static class ResourceAsyncScope
{
    internal static async Task<Outcome<T>> ExecuteAsync<TResource, T>(
        Ast.IInterpreter interpreter,
        Options options,
        TResource resource,
        Func<TResource, IFlow<T>> use)
        where TResource : IAsyncDisposable
    {
        Outcome<T>? outcome = null;
        Exception? disposeException = null;

        try
        {
            Ast.INode<T>? inner = null;
            try
            {
                inner = use(resource).AsNode();
            }
            catch (Exception useException)
            {
                outcome = Outcome.Failure<T>(useException);
            }

            if (inner is not null)
            {
                outcome = await inner.Accept(interpreter).ConfigureAwait(false);
            }
        }
        finally
        {
            if (!options.CancellationToken.IsCancellationRequested)
            {
                try
                {
                    await resource.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    disposeException = ex;
                }
            }
            else
            {
                var t = Task.Run(async () =>
                {
                    try
                    {
                        await resource.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _ = ex;
                    }
                });
                t.ObserveFaults();
            }
        }

        return disposeException is null
            ? outcome ?? Outcome.Failure<T>(new InvalidOperationException("WithResourceAsync produced no outcome."))
            : Outcome.Failure<T>(disposeException);
    }
}
