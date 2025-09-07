using BahmanM.Flow.Support;

namespace BahmanM.Flow.Execution.Primitive;

internal class Any(Ast.IInterpreter interpreter, Options options)
{
    internal Task<Outcome<T>> Interpret<T>(Ast.Primitive.Any<T> node) =>
        AnyExecutor<T>.ExecuteAsync(node, options);

    private static class AnyExecutor<T>
    {
        internal static async Task<Outcome<T>> ExecuteAsync(Ast.Primitive.Any<T> node, Options options)
        {
            using var cts = CreateLinkedCts(options);
            var childInterpreter = CreateChildInterpreter(cts);
            var tasks = StartAll(node, childInterpreter);

            var (success, failures) = await DrainUntilFirstSuccess(tasks, cts);
            return success ?? Outcome.Failure<T>(new AggregateException(failures));
        }

        private static CancellationTokenSource CreateLinkedCts(Options options) =>
            CancellationTokenSource.CreateLinkedTokenSource(options.CancellationToken);

        private static Execution.Interpreter CreateChildInterpreter(CancellationTokenSource cts) =>
            new(new Options(cts.Token));

        private static List<Task<Outcome<T>>> StartAll(Ast.Primitive.Any<T> node, Ast.IInterpreter child) =>
            node.Flows
                .Select(f => f.AsNode().Accept(child))
                .ToList();

        private static async Task<(Success<T>? success, List<Exception> failures)> DrainUntilFirstSuccess(
            List<Task<Outcome<T>>> tasks, CancellationTokenSource cts)
        {
            var failures = new List<Exception>();

            while (tasks.Count > 0)
            {
                var completed = await Task.WhenAny(tasks);
                tasks.Remove(completed);

                var outcome = await completed;
                switch (outcome)
                {
                    case Success<T> s:
                        CancelRemaining(cts, tasks);
                        return (s, failures);
                    case Failure<T> f:
                        failures.Add(f.Exception);
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported outcome type: {outcome.GetType().Name}");
                }
            }

            return (null, failures);
        }

        private static void CancelRemaining(CancellationTokenSource cts, List<Task<Outcome<T>>> remaining)
        {
            TryCancel(cts);
            ObserveRemainingFaults(remaining);
        }

        private static void TryCancel(CancellationTokenSource cts)
        {
            try
            {
                cts.Cancel();
            }
            catch
            {
                // ignore
            }
        }

        private static void ObserveRemainingFaults(List<Task<Outcome<T>>> remaining)
        {
            if (remaining.Count == 0)
                return;

            _ = Task
                .WhenAll(remaining)
                .ContinueWith(
                    t => { _ = t.Exception; },
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
