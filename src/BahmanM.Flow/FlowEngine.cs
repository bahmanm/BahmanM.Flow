namespace BahmanM.Flow;

public static class FlowEngine
{
    public static async Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow)
    {
        var continuations = new Stack<object>();
        var currentFlow = flow;

        // 1. Deconstruct the flow into a source and a stack of continuations
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
                // Other operators will be added here in the future
            }
            break;
        }

        // 2. Execute the source flow to get the initial outcome
        var outcome = await ExecuteSourceFlow(currentFlow);

        // 3. Execute the continuations iteratively
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
                    // This case will be expanded for other operators like Select, Chain etc.
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
