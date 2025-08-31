namespace BahmanM.Flow.Execution.Resource;

internal class WithResource(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<T>> Interpret<TResource, T>(Ast.Resource.WithResource<TResource, T> node)
        where TResource : IDisposable
    {
        TResource resource;
        Outcome<T>? outcome = null;
        Exception? disposeException = null;

        // Acquire
        try
        {
            resource = node.Acquire();
        }
        catch (Exception acquireEx)
        {
            return Outcome.Failure<T>(acquireEx);
        }

        try
        {
            Ast.INode<T>? inner = null;
            try
            {
                inner = (Ast.INode<T>)node.Use(resource);
            }
            catch (Exception useEx)
            {
                outcome = Outcome.Failure<T>(useEx);
            }

            if (inner is not null)
            {
                outcome = await inner.Accept(interpreter);
            }
        }
        finally
        {
            try
            {
                // Ensure exactly-once disposal for acquired resource
                resource.Dispose();
            }
            catch (Exception ex)
            {
                disposeException = ex;
            }
        }

        return disposeException is null
            ? outcome ?? Outcome.Failure<T>(new InvalidOperationException("WithResource produced no outcome."))
            : Outcome.Failure<T>(disposeException);
    }
}
