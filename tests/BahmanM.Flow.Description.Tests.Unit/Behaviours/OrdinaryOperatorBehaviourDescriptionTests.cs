using BahmanM.Flow.Description.Serialisation;

namespace BahmanM.Flow.Description.Tests.Unit.Behaviours;

public class OrdinaryOperatorBehaviourDescriptionTests
{
    [Theory]
    [InlineData(OperatorKind.DoOnSuccess, OperatorVariant.Sync)]
    [InlineData(OperatorKind.DoOnSuccess, OperatorVariant.Async)]
    [InlineData(OperatorKind.DoOnSuccess, OperatorVariant.CancellableAsync)]
    [InlineData(OperatorKind.DoOnFailure, OperatorVariant.Sync)]
    [InlineData(OperatorKind.DoOnFailure, OperatorVariant.Async)]
    [InlineData(OperatorKind.DoOnFailure, OperatorVariant.CancellableAsync)]
    [InlineData(OperatorKind.Select, OperatorVariant.Sync)]
    [InlineData(OperatorKind.Select, OperatorVariant.Async)]
    [InlineData(OperatorKind.Select, OperatorVariant.CancellableAsync)]
    [InlineData(OperatorKind.Validate, OperatorVariant.Sync)]
    [InlineData(OperatorKind.Validate, OperatorVariant.Async)]
    [InlineData(OperatorKind.Validate, OperatorVariant.CancellableAsync)]
    public async Task Retry_Description_AroundOrdinaryOperatorDoesNotRetryItsUpstream(
        OperatorKind kind,
        OperatorVariant variant)
    {
        var sourceException = new InvalidOperationException("source failed");
        var sourceCalls = 0;
        var operatorCalls = 0;
        Flow.Operations.Create.Sync<int> operation = () =>
        {
            sourceCalls++;
            throw sourceException;
        };
        var source = (Flow.Create<int>(() => operation())).AsDescription();
        var wrapped = Wrap(source, kind, variant, () => operatorCalls++);
        var description = ((wrapped).WithRetry(maxAttempts: 3)).AsDescription();

        var text = DescriptionFormatter.Describe(description);
        var lowered = description.Lower();
        Assert.Contains("Retry<int>", text);
        Assert.Contains(OperatorLabel(kind, variant), text);
        Assert.Equal(0, sourceCalls);
        Assert.Equal(0, operatorCalls);
        Assert.IsAssignableFrom<Ast.INode<int>>(lowered);

        var outcome = await FlowEngine.ExecuteAsync(description);

        Assert.Same(sourceException, Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(1, sourceCalls);
        Assert.Equal(kind == OperatorKind.DoOnFailure ? 1 : 0, operatorCalls);
    }

    [Theory]
    [InlineData(OperatorKind.DoOnSuccess, OperatorVariant.Sync)]
    [InlineData(OperatorKind.DoOnSuccess, OperatorVariant.Async)]
    [InlineData(OperatorKind.DoOnSuccess, OperatorVariant.CancellableAsync)]
    [InlineData(OperatorKind.DoOnFailure, OperatorVariant.Sync)]
    [InlineData(OperatorKind.DoOnFailure, OperatorVariant.Async)]
    [InlineData(OperatorKind.DoOnFailure, OperatorVariant.CancellableAsync)]
    [InlineData(OperatorKind.Select, OperatorVariant.Sync)]
    [InlineData(OperatorKind.Select, OperatorVariant.Async)]
    [InlineData(OperatorKind.Select, OperatorVariant.CancellableAsync)]
    [InlineData(OperatorKind.Validate, OperatorVariant.Sync)]
    [InlineData(OperatorKind.Validate, OperatorVariant.Async)]
    [InlineData(OperatorKind.Validate, OperatorVariant.CancellableAsync)]
    public async Task Timeout_Description_AroundOrdinaryOperatorTimesItsUpstream(
        OperatorKind kind,
        OperatorVariant variant)
    {
        var sourceStarted = NewSignal();
        var sourceCancelled = NewSignal();
        var sourceCalls = 0;
        var operatorCalls = 0;
        CancellationToken operatorToken = default;
        Flow.Operations.Create.CancellableAsync<int> operation = async token =>
        {
            sourceCalls++;
            sourceStarted.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return 42;
            }
            catch (OperationCanceledException)
            {
                sourceCancelled.TrySetResult();
                throw;
            }
        };
        var source = (Flow.Create<int>(cancellationToken => operation(cancellationToken))).AsDescription();
        var wrapped = Wrap(source, kind, variant, () => operatorCalls++, token => operatorToken = token);
        var description = ((wrapped).WithTimeout(TimeSpan.FromMilliseconds(300))).AsDescription();

        var text = DescriptionFormatter.Describe(description);
        var lowered = description.Lower();
        Assert.Contains("Timeout<int>", text);
        Assert.Contains(OperatorLabel(kind, variant), text);
        Assert.Equal(0, sourceCalls);
        Assert.Equal(0, operatorCalls);
        Assert.IsAssignableFrom<Ast.INode<int>>(lowered);

        var execution = FlowEngine.ExecuteAsync(description);
        await sourceStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2));
        await sourceCancelled.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(1, sourceCalls);
        Assert.Equal(kind == OperatorKind.DoOnFailure ? 1 : 0, operatorCalls);
        if (kind == OperatorKind.DoOnFailure && variant == OperatorVariant.CancellableAsync)
        {
            Assert.Equal(CancellationToken.None, operatorToken);
            Assert.False(operatorToken.IsCancellationRequested);
        }
    }

    [Fact]
    public async Task Timeout_Description_AroundSelectDoesNotBoundSelectOperation()
    {
        var selectStarted = NewSignal();
        var releaseSelect = NewSignal();
        var timeout = TimeSpan.FromMilliseconds(300);
        var selectToken = CancellationToken.None;
        Flow.Operations.Select.CancellableAsync<int, int> select = async (value, token) =>
        {
            selectToken = token;
            selectStarted.TrySetResult();
            await releaseSelect.Task;
            return value;
        };
        var description = (((((Flow.Create(() => 42)).AsDescription()).Select(select)).AsDescription()).WithTimeout(timeout)).AsDescription();

        var execution = FlowEngine.ExecuteAsync(description);
        await selectStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await Task.Delay(timeout + timeout);
        var executionRemainsPending = !execution.IsCompleted;
        releaseSelect.TrySetResult();
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(executionRemainsPending);
        Assert.Equal(CancellationToken.None, selectToken);
        Assert.Equal(Outcome.Success(42), outcome);
    }

    private static IDescription<int> Wrap(
        IDescription<int> source,
        OperatorKind kind,
        OperatorVariant variant,
        Action? onOperator = null,
        Action<CancellationToken>? captureToken = null)
    {
        onOperator ??= () => { };
        var exception = new InvalidOperationException("operator failure");

        return (kind, variant) switch
        {
            (OperatorKind.DoOnSuccess, OperatorVariant.Sync) => ((source).DoOnSuccess(_ => onOperator())).AsDescription(),
            (OperatorKind.DoOnSuccess, OperatorVariant.Async) => ((source).DoOnSuccess(async _ =>
                {
                    onOperator();
                    await Task.Yield();
                })).AsDescription(),
            (OperatorKind.DoOnSuccess, OperatorVariant.CancellableAsync) => ((source).DoOnSuccess(async (_, token) =>
                {
                    captureToken?.Invoke(token);
                    onOperator();
                    await Task.Yield();
                })).AsDescription(),
            (OperatorKind.DoOnFailure, OperatorVariant.Sync) => ((source).DoOnFailure(_ => onOperator())).AsDescription(),
            (OperatorKind.DoOnFailure, OperatorVariant.Async) => ((source).DoOnFailure(async _ =>
                {
                    onOperator();
                    await Task.Yield();
                })).AsDescription(),
            (OperatorKind.DoOnFailure, OperatorVariant.CancellableAsync) => ((source).DoOnFailure(async (_, token) =>
                {
                    captureToken?.Invoke(token);
                    onOperator();
                    await Task.Yield();
                })).AsDescription(),
            (OperatorKind.Select, OperatorVariant.Sync) => ((source).Select(value =>
                {
                    onOperator();
                    return value;
                })).AsDescription(),
            (OperatorKind.Select, OperatorVariant.Async) => ((source).Select<int, int>(async value =>
                {
                    onOperator();
                    await Task.Yield();
                    return value;
                })).AsDescription(),
            (OperatorKind.Select, OperatorVariant.CancellableAsync) => ((source).Select(async (value, token) =>
                {
                    captureToken?.Invoke(token);
                    onOperator();
                    await Task.Yield();
                    return value;
                })).AsDescription(),
            (OperatorKind.Validate, OperatorVariant.Sync) => ((source).Validate(_ =>
                {
                    onOperator();
                    return true;
                }, _ => exception)).AsDescription(),
            (OperatorKind.Validate, OperatorVariant.Async) => ((source).Validate(async _ =>
                {
                    onOperator();
                    await Task.Yield();
                    return true;
                }, _ => exception)).AsDescription(),
            (OperatorKind.Validate, OperatorVariant.CancellableAsync) => ((source).Validate(async (_, token) =>
                {
                    captureToken?.Invoke(token);
                    onOperator();
                    await Task.Yield();
                    return true;
                }, _ => exception)).AsDescription(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static string OperatorLabel(OperatorKind kind, OperatorVariant variant) =>
        kind == OperatorKind.Select
            ? $"Select.{variant}<int, int>"
            : $"{kind}.{variant}<int>";

    public enum OperatorKind
    {
        DoOnSuccess,
        DoOnFailure,
        Select,
        Validate
    }

    public enum OperatorVariant
    {
        Sync,
        Async,
        CancellableAsync
    }
}
