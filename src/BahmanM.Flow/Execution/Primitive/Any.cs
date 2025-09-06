namespace BahmanM.Flow.Execution.Primitive;

internal class Any(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<T>> Interpret<T>(Ast.Primitive.Any<T> node)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(options.CancellationToken);

        var childInterpreter = new Execution.Interpreter(new Options(cts.Token));

        var tasks = node
            .Flows
            .Select(f =>
                ((Ast.INode<T>)f).Accept(childInterpreter))
            .ToList();

        var exceptions = new List<Exception>();

        while (tasks.Count > 0)
        {
            var completed = await Task.WhenAny(tasks);
            tasks.Remove(completed);

            var outcome = await completed;
            if (outcome is Success<T> s)
            {
                // Signal cancellation to remaining subflows and observe their completions
                // without delaying the successful result.
                try { cts.Cancel(); }
                catch { /* ignore */ }

                if (tasks.Count > 0)
                {
                    _ = Task.WhenAll(tasks)
                        .ContinueWith(t => { var _ = t.Exception; },
                            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
                }

                return s;
            }

            if (outcome is Failure<T> f)
            {
                exceptions.Add(f.Exception);
            }
            else
            {
                throw new NotSupportedException($"Unsupported outcome type: {outcome.GetType().Name}");
            }
        }

        return Outcome.Failure<T>(new AggregateException(exceptions));
    }
}
