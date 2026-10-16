using BahmanM.Flow.Behaviour;
using BahmanM.Flow.Description.Serialisation;

namespace BahmanM.Flow.Description.Tests.Unit.Behaviours;

public class CustomBehaviourDescriptionTests
{
    [Fact]
    public async Task CustomBehaviour_DescriptionRetainsType_AndDefersApplyUntilLowering()
    {
        var sourceCalls = 0;
        var behaviour = new RecordingBehaviour("Test.Counting", () => { });
        Flow.Operations.Create.Sync<int> operation = () =>
        {
            sourceCalls++;
            return 42;
        };
        var source = (Flow.Create(() => operation())).AsDescription();
        var description = new Behaviour.Custom.Custom<int>(source, behaviour);

        var text = DescriptionFormatter.Describe(description);
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);

        Assert.NotEqual(source.Id, description.Id);
        Assert.Contains("CustomBehaviour<int>", text);
        Assert.Contains("operationType: Test.Counting", text);
        Assert.Contains("Create.Sync<int>", text);
        Assert.IsType<Ast.Create.Sync<int>>(lowered);
        Assert.Equal(1, behaviour.ApplyCalls);
        Assert.Equal(0, sourceCalls);

        var outcome = await FlowEngine.ExecuteAsync(lowered);

        Assert.Equal(Outcome.Success(42), outcome);
        Assert.Equal(1, behaviour.ApplyCalls);
        Assert.Equal(1, sourceCalls);
    }

    [Fact]
    public async Task CustomBehaviour_LoweringAppliesOnceAndReLoweringObservesState()
    {
        var sourceCalls = 0;
        Flow.Operations.Create.Sync<int> operation = () =>
        {
            sourceCalls++;
            return 42;
        };
        var behaviour = new FailSecondApplicationBehaviour(new InvalidOperationException("second application"));
        var description = new Behaviour.Custom.Custom<int>((Flow.Create(() => operation())).AsDescription(), behaviour);
        Assert.Equal(0, behaviour.ApplyCalls);
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        Assert.Equal(1, behaviour.ApplyCalls);
        var first = await FlowEngine.ExecuteAsync(lowered);
        var secondLowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        var second = await FlowEngine.ExecuteAsync(secondLowered);

        Assert.Equal(Outcome.Success(42), first);
        Assert.Same(behaviour.SecondApplicationException, Assert.IsType<Failure<int>>(second).Exception);
        Assert.Equal(2, behaviour.ApplyCalls);
        Assert.Equal(1, sourceCalls);
    }

    [Fact]
    public async Task CustomBehaviour_ApplyReceivesLoweredAst()
    {
        var behaviour = new DescriptionSourceBehaviour();
        var description = new Behaviour.Custom.Custom<int>((Flow.Succeed(42)).AsDescription(), behaviour);
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);

        Assert.True(behaviour.ReceivedAstNode);
        var outcome = await FlowEngine.ExecuteAsync(lowered);

        Assert.Equal(Outcome.Success(42), outcome);
        Assert.True(behaviour.ReceivedAstNode);
    }

    [Fact]
    public async Task CustomBehaviour_LoweringAppliesConcurrently()
    {
        var sourceCalls = 0;
        Flow.Operations.Create.Sync<int> operation = () => Interlocked.Increment(ref sourceCalls);
        using var barrier = new Barrier(2);
        var behaviour = new RecordingBehaviour(
            "Test.Concurrent",
            () => Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(2))));
        var firstLowering = Task.Run(() => new Behaviour.Custom.Custom<int>((Flow.Create(() => operation())).AsDescription(), behaviour).Lower(FacadeBehaviourStrategyFactory.Instance));
        var secondLowering = Task.Run(() => new Behaviour.Custom.Custom<int>((Flow.Create(() => operation())).AsDescription(), behaviour).Lower(FacadeBehaviourStrategyFactory.Instance));
        var loweredFlows = await Task.WhenAll(firstLowering, secondLowering);
        var outcomes = await Task.WhenAll(
            FlowEngine.ExecuteAsync(loweredFlows[0]),
            FlowEngine.ExecuteAsync(loweredFlows[1]));

        Assert.Equal(2, behaviour.ApplyCalls);
        Assert.Equal(2, sourceCalls);
        Assert.Equal(new[] { 1, 2 }, outcomes.OfType<Success<int>>().Select(outcome => outcome.Value).Order().ToArray());
        Assert.All(outcomes, outcome => Assert.IsType<Success<int>>(outcome));
    }

    [Fact]
    public async Task CustomBehaviour_ComposesInApplicationAndExecutionOrder()
    {
        var events = new List<string>();
        Flow.Operations.Create.Sync<int> operation = () =>
        {
            events.Add("Source");
            return 42;
        };
        var source = (Flow.Create(() => operation())).AsDescription();
        var inner = new Behaviour.Custom.Custom<int>(source, new WrappingBehaviour("Inner", events));
        var outer = new Behaviour.Custom.Custom<int>(inner, new WrappingBehaviour("Outer", events));

        var text = DescriptionFormatter.Describe(outer);
        Assert.Contains("operationType: Test.Outer", text);
        Assert.Contains("operationType: Test.Inner", text);
        Assert.Empty(events);
        var lowered = outer.Lower(FacadeBehaviourStrategyFactory.Instance);
        Assert.Equal(new[] { "Inner.Apply", "Outer.Apply" }, events);

        var outcome = await FlowEngine.ExecuteAsync(lowered);

        Assert.Equal(Outcome.Success(42), outcome);
        Assert.Equal(
            new[] { "Inner.Apply", "Outer.Apply", "Source", "Inner.Execute", "Outer.Execute" },
            events);
    }

    [Fact]
    public void CustomBehaviour_ApplyExceptionIsThrownDuringLowering()
    {
        var expectedException = new InvalidOperationException("apply failed");
        var behaviour = new RecordingBehaviour("Test.Throwing", () => throw expectedException);
        var description = new Behaviour.Custom.Custom<int>((Flow.Succeed(42)).AsDescription(), behaviour);

        var exception = Assert.Throws<InvalidOperationException>(() => description.Lower(FacadeBehaviourStrategyFactory.Instance));

        Assert.Same(expectedException, exception);
        Assert.Equal(1, behaviour.ApplyCalls);
    }

    [Fact]
    public async Task CustomBehaviour_CanReturnDescriptionBackedFlow()
    {
        var behaviour = new DescriptionReturningBehaviour();
        Flow.Operations.Create.Sync<int> operation = () =>
            throw new Xunit.Sdk.XunitException("Original flow should be replaced.");
        var description = new Behaviour.Custom.Custom<int>((Flow.Create(() => operation())).AsDescription(), behaviour);

        var outcome = await FlowEngine.ExecuteAsync(description);

        Assert.Equal(Outcome.Success(0), outcome);
        Assert.Equal(1, behaviour.ApplyCalls);
    }

    [Fact]
    public async Task CustomBehaviour_ReturnedCancellableDescriptionReceivesExecutionToken()
    {
        using var cancellation = new CancellationTokenSource();
        var operationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var receivedToken = CancellationToken.None;
        var behaviour = new CancellableDescriptionReturningBehaviour(token =>
        {
            receivedToken = token;
            operationStarted.TrySetResult();
        });
        var description = new Behaviour.Custom.Custom<int>((Flow.Succeed(1)).AsDescription(), behaviour);

        var execution = FlowEngine.ExecuteAsync(description, new Execution.Options(cancellation.Token));
        await operationStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        Assert.Equal(1, behaviour.ApplyCalls);
        Assert.Equal(cancellation.Token, receivedToken);
        Assert.True(receivedToken.IsCancellationRequested);
        Assert.IsAssignableFrom<OperationCanceledException>(Assert.IsType<Failure<int>>(outcome).Exception);
    }

    private sealed class RecordingBehaviour(string operationType, Action onApply) : IBehaviour
    {
        private int _applyCalls;

        public string OperationType { get; } = operationType;
        public int ApplyCalls => Volatile.Read(ref _applyCalls);

        public IFlow<T> Apply<T>(IFlow<T> originalFlow)
        {
            Interlocked.Increment(ref _applyCalls);
            onApply();
            return originalFlow;
        }
    }

    private sealed class FailSecondApplicationBehaviour(Exception secondApplicationException) : IBehaviour
    {
        private int _applyCalls;

        public string OperationType => "Test.StateDependent";
        public int ApplyCalls => Volatile.Read(ref _applyCalls);
        public Exception SecondApplicationException { get; } = secondApplicationException;

        public IFlow<T> Apply<T>(IFlow<T> originalFlow) =>
            Interlocked.Increment(ref _applyCalls) == 2
                ? Flow.Fail<T>(SecondApplicationException)
                : originalFlow;
    }

    private sealed class WrappingBehaviour(string name, List<string> events) : IBehaviour
    {
        public string OperationType => $"Test.{name}";

        public IFlow<T> Apply<T>(IFlow<T> originalFlow)
        {
            events.Add($"{name}.Apply");
            return originalFlow.DoOnSuccess(_ => events.Add($"{name}.Execute"));
        }
    }

    private sealed class DescriptionReturningBehaviour : IBehaviour
    {
        private int _applyCalls;

        public string OperationType => "Test.DescriptionReturning";
        public int ApplyCalls => Volatile.Read(ref _applyCalls);

        public IFlow<T> Apply<T>(IFlow<T> originalFlow)
        {
            Interlocked.Increment(ref _applyCalls);
            return (Flow.Succeed<T>(default!)).AsDescription();
        }
    }

    private sealed class DescriptionSourceBehaviour : IBehaviour
    {
        public string OperationType => "Test.DescriptionSource";
        public bool ReceivedAstNode { get; private set; }

        public IFlow<T> Apply<T>(IFlow<T> originalFlow)
        {
            ReceivedAstNode = originalFlow is Ast.INode<T>;
            return originalFlow;
        }
    }

    private sealed class CancellableDescriptionReturningBehaviour(Action<CancellationToken> onStart) : IBehaviour
    {
        private int _applyCalls;

        public string OperationType => "Test.CancellableDescriptionReturning";
        public int ApplyCalls => Volatile.Read(ref _applyCalls);

        public IFlow<T> Apply<T>(IFlow<T> originalFlow)
        {
            Interlocked.Increment(ref _applyCalls);
            Flow.Operations.Create.CancellableAsync<T> operation = async token =>
            {
                onStart(token);
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return default!;
            };
            return (Flow.Create<T>(cancellationToken => operation(cancellationToken))).AsDescription();
        }
    }
}
