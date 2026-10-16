using BahmanM.Flow.Behaviour;
using BahmanM.Flow.Description;
using BahmanM.Flow.Description.Serialisation;
using DescriptionBehaviour = BahmanM.Flow.Description.Behaviour;
using DoOnSuccess = BahmanM.Flow.Description.DoOnSuccess;
using Primitive = BahmanM.Flow.Description.Primitive;

namespace BahmanM.Flow.Tests.Unit;

public class PublicDslBehaviourTests
{
    [Fact]
    public async Task WithBehaviour_AppliesAtCompositionAndRetainsItsResultForExecution()
    {
        var source = Flow.Succeed(42);
        var behaviour = new CountingBehaviour();

        var flow = source.WithBehaviour(behaviour);

        var description = Assert.IsType<DescriptionBehaviour.Custom.Custom<int>>(flow);
        Assert.Same(source, description.Source);
        Assert.IsType<DoOnSuccess.Sync<int>>(description.CachedResultDescription);
        Assert.Equal(1, behaviour.ApplyCount);

        var rendered = DescriptionFormatter.Describe(description);
        Assert.Contains("CustomBehaviour<int>", rendered);
        Assert.Contains("operationType: test.counting", rendered);
        Assert.Contains("source: Succeed<int>", rendered);
        Assert.Contains("cachedResultDescription: DoOnSuccess.Sync<int>", rendered);
        Assert.Equal(1, behaviour.ApplyCount);

        Assert.Equal(new Success<int>(42), await FlowEngine.ExecuteAsync(flow));
        Assert.Equal(new Success<int>(42), await FlowEngine.ExecuteAsync(flow));
        Assert.Equal(1, behaviour.ApplyCount);
        Assert.Equal(2, behaviour.ExecutionCount);
    }

    [Fact]
    public void WithBehaviour_PreservesCompositionTimeExceptions()
    {
        var expected = new InvalidOperationException("apply failed");
        var behaviour = new ThrowingBehaviour(expected);

        var actual = Assert.Throws<InvalidOperationException>(
            () => Flow.Succeed(1).WithBehaviour(behaviour));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task WithRetry_UsesDescriptionAndPreservesPublicExecutionSemantics()
    {
        var invocationCount = 0;
        var flow = Flow.Create(() =>
        {
            invocationCount++;
            if (invocationCount < 3)
                throw new InvalidOperationException("retry");
            return 42;
        }).WithRetry(3);

        var description = Assert.IsType<DescriptionBehaviour.Retry.Retry<int>>(flow);
        Assert.Equal(3, description.MaxAttempts);
        Assert.Contains(typeof(TimeoutException), description.NonRetryableExceptions);
        Assert.Equal(new Success<int>(42), await FlowEngine.ExecuteAsync(flow));
        Assert.Equal(3, invocationCount);
    }

    [Fact]
    public async Task WithTimeout_UsesDescriptionAndExecutesThroughPublicEngine()
    {
        var duration = TimeSpan.FromSeconds(1);
        var flow = Flow.Create(() => 42).WithTimeout(duration);

        var description = Assert.IsType<DescriptionBehaviour.Timeout.Timeout<int>>(flow);
        Assert.Equal(duration, description.Duration);
        Assert.Equal(new Success<int>(42), await FlowEngine.ExecuteAsync(flow));
    }

    [Fact]
    public async Task WithBehaviour_RetainsAstBackedResult()
    {
        var source = Flow.Create(() => 42);
        var flow = source.WithBehaviour(new IdentityBehaviour());

        var description = Assert.IsType<DescriptionBehaviour.Custom.Custom<int>>(flow);
        Assert.IsType<ExistingFlow<int>>(description.CachedResultDescription);
        Assert.Equal(new Success<int>(42), await FlowEngine.ExecuteAsync(flow));
    }

    [Fact]
    public async Task WithBehaviour_RetainsDescriptionBackedReplacementResult()
    {
        var flow = Flow.Create(() => 1).WithBehaviour(new ReplacementBehaviour<int>(42));

        var description = Assert.IsType<DescriptionBehaviour.Custom.Custom<int>>(flow);
        Assert.IsType<Primitive.Succeed<int>>(description.CachedResultDescription);
        Assert.Equal(new Success<int>(42), await FlowEngine.ExecuteAsync(flow));
    }

    [Fact]
    public void BehaviourDescriptions_PreserveDeclaredNestingAndInspectionIsInert()
    {
        var operationCount = 0;
        var duration = TimeSpan.FromSeconds(2);
        var flow = Flow.Create(() => ++operationCount)
            .WithBehaviour(new IdentityBehaviour())
            .WithRetry(3, typeof(InvalidOperationException))
            .WithTimeout(duration);

        var rendered = DescriptionFormatter.Describe(Assert.IsAssignableFrom<IDescription<int>>(flow));

        Assert.True(rendered.IndexOf("Timeout<int>", StringComparison.Ordinal) < rendered.IndexOf("Retry<int>", StringComparison.Ordinal));
        Assert.True(rendered.IndexOf("Retry<int>", StringComparison.Ordinal) < rendered.IndexOf("CustomBehaviour<int>", StringComparison.Ordinal));
        Assert.Contains("duration: 00:00:02", rendered);
        Assert.Contains("maxAttempts: 3", rendered);
        Assert.Contains("InvalidOperationException", rendered);
        Assert.Equal(0, operationCount);
    }

    private sealed class CountingBehaviour : IBehaviour
    {
        public string OperationType => "test.counting";
        public int ApplyCount { get; private set; }
        public int ExecutionCount { get; private set; }

        public IFlow<T> Apply<T>(IFlow<T> originalFlow)
        {
            ApplyCount++;
            return originalFlow.DoOnSuccess(_ => ExecutionCount++);
        }
    }

    private sealed class ThrowingBehaviour(Exception exception) : IBehaviour
    {
        public string OperationType => "test.throwing";

        public IFlow<T> Apply<T>(IFlow<T> originalFlow) => throw exception;
    }

    private sealed class IdentityBehaviour : IBehaviour
    {
        public string OperationType => "test.identity";
        public IFlow<T> Apply<T>(IFlow<T> originalFlow) => originalFlow;
    }

    private sealed class ReplacementBehaviour<T>(T value) : IBehaviour
    {
        public string OperationType => "test.replacement";
        public IFlow<TResult> Apply<TResult>(IFlow<TResult> originalFlow) =>
            typeof(TResult) == typeof(T)
                ? (IFlow<TResult>)Flow.Succeed(value)
                : throw new InvalidOperationException("Unexpected flow type.");
    }
}
