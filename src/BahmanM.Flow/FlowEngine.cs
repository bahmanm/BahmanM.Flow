namespace BahmanM.Flow;

// The FlowEngine is the interpreter that executes a flow. It is implemented using the Trampoline pattern
// to prevent stack overflow exceptions when executing deeply nested flows.
// See: https://en.wikipedia.org/wiki/Trampoline_(computing)
public static class FlowEngine
{
    // This method is intentionally imperative and uses `break` to achieve stack safety.
    // A recursive approach would be more declarative but would cause a StackOverflowException on long chains
    // in languages without tail-call optimization. This is the core of the Trampoline pattern.
    public static async Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow)
    {
        var continuations = new Stack<object>();
        var currentFlow = flow;
        
        while (true)
        {
            switch (currentFlow)
            {
                case DoOnSuccessFlow<T> doOnSuccess:
                    continuations.Push(doOnSuccess.Action);
                    currentFlow = doOnSuccess.Upstream;
                    continue;
                case AsyncDoOnSuccessFlow<T> asyncDoOnSuccess:
                    continuations.Push(asyncDoOnSuccess.AsyncAction);
                    currentFlow = asyncDoOnSuccess.Upstream;
                    continue;
            }
            break;
        }
        
        var outcome = await ExecuteSourceFlow(currentFlow);
        
        while (continuations.Count > 0)
        {
            if (outcome is Failure<T>)
            {
                // Once the flow has failed, skip all remaining success-based continuations
                break;
            }

            var continuation = continuations.Pop();
            var successValue = ((Success<T>)outcome).Value;

            outcome = await ApplyContinuation(continuation, successValue);
        }

        return outcome;
    }

    private static async Task<Outcome<T>> ExecuteSourceFlow<T>(IFlow<T> sourceFlow)
    {
        return sourceFlow switch
        {
            SucceededFlow<T> s => Outcome.Success(s.Value),
            FailedFlow<T> f => Outcome.Failure<T>(f.Exception),
            CreateFlow<T> c => await TryOperation(c.Operation),
            AsyncCreateFlow<T> ac => await TryOperation(ac.Operation),
            _ => throw new NotSupportedException($"Unsupported source flow type: {sourceFlow.GetType().Name}")
        };
    }

    private static async Task<Outcome<T>> ApplyContinuation<T>(object continuation, T value)
    {
        try
        {
            switch (continuation)
            {
                case Action<T> action:
                    action(value);
                    break;
                case Func<T, Task> asyncAction:
                    await asyncAction(value);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported continuation type: {continuation.GetType().Name}");
            }
            return Outcome.Success(value);
        }
        catch (Exception ex)
        {
            return Outcome.Failure<T>(ex);
        }
    }

    private static Task<Outcome<T>> TryOperation<T>(Func<T> operation)
    {
        try
        {
            return Task.FromResult(Outcome.Success(operation()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Outcome.Failure<T>(ex));
        }
    }

    private static async Task<Outcome<T>> TryOperation<T>(Func<Task<T>> operation)
    {
        try
        {
            return Outcome.Success(await operation());
        }
        catch (Exception ex)
        {
            return Outcome.Failure<T>(ex);
        }
    }
}
