using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Unit
{
    public class WithRetryTests
    {
        // Al-Biruni (c. 973–1050) was a Persian polymath of the Islamic Golden Age. He was a
        // scholar in physics, mathematics, astronomy, and natural sciences, and
        // also distinguished himself as a historian, chronologist and linguist.
        private const string AlBiruni = "Al-Biruni";

        public class NonFailableFlowsTheoryData : TheoryData<IFlow<string>>
        {
            public NonFailableFlowsTheoryData()
            {
                Add(Flow.Succeed("succeeded"));
                Add(Flow.Fail<string>(new Exception("dummy")));
                Add(Flow.Succeed("s").Select(_ => "selected"));
                Add(Flow.Succeed("s").Select(async _ => { await Task.Delay(1); return "async selected"; }));
                Add(Flow.Succeed("s").DoOnSuccess(_ => { }));
                Add(Flow.Succeed("s").DoOnSuccess(async _ => await Task.Delay(1)));
                Add(Flow.Succeed("s").DoOnFailure(_ => { }));
                Add(Flow.Succeed("s").DoOnFailure(async _ => await Task.Delay(1)));
            }
        }


        [Theory]
        [ClassData(typeof(NonFailableFlowsTheoryData))]
        public void WithRetry_OnNonFailableNodes_IsANoOpAndReturnsSameInstance(IFlow<string> nonFailableFlow)
        {
            // Arrange
            var originalFlow = nonFailableFlow;

            // Act
            var resultFlow = originalFlow.WithRetry(3);

            // Assert
            Assert.Equal(originalFlow, resultFlow);
        }

        [Fact]
        public async Task WithRetry_OnFlakyOperation_SucceedsAfterRetries()
        {
            // Arrange
            var attempts = 0;
            var flakyFlow = Flow.Create<string>(() =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new InvalidOperationException("Service is not ready yet.");
                }
                return AlBiruni;
            });

            var resilientFlow = flakyFlow.WithRetry(3);

            // Act
            var outcome = await FlowEngine.ExecuteAsync(resilientFlow);

            // Assert
            Assert.Equal(3, attempts);
            Assert.Equal(Success(AlBiruni), outcome);
        }

        [Fact]
        public async Task WithRetry_OnSuccessfulFirstAttempt_ReturnsSuccess()
        {
            // Arrange
            var attempts = 0;
            var stableFlow = Flow.Create<string>(() =>
            {
                attempts++;
                return AlBiruni;
            });

            var resilientFlow = stableFlow.WithRetry(3);

            // Act
            var outcome = await FlowEngine.ExecuteAsync(resilientFlow);

            // Assert
            Assert.Equal(1, attempts);
            Assert.Equal(Success(AlBiruni), outcome);
        }

        [Fact]
        public async Task WithRetry_DoesNotReExecuteUpstreamSideEffects()
        {
            // Arrange
            var sideEffectCount = 0;
            var flakyAttempts = 0;

            var flowWithSideEffect = Flow.Succeed("start")
                .DoOnSuccess(_ => sideEffectCount++);

            var flakyFlow = flowWithSideEffect.Chain(_ => Flow.Create(() =>
            {
                flakyAttempts++;
                if (flakyAttempts < 3)
                {
                    throw new InvalidOperationException("Flaky part failed.");
                }
                return "final value";
            }));

            var resilientFlow = flakyFlow.WithRetry(3);

            // Act
            var outcome = await FlowEngine.ExecuteAsync(resilientFlow);

            // Assert
            Assert.Equal(1, sideEffectCount);
            Assert.Equal(3, flakyAttempts);
            Assert.Equal(Success("final value"), outcome);
        }

        [Fact]
        public async Task WithRetry_OnFlakyAsyncOperation_SucceedsAfterRetries()
        {
            // Arrange
            var attempts = 0;
            var flakyFlow = Flow.Create(async () =>
            {
                attempts++;
                await Task.Delay(1);
                if (attempts < 3)
                {
                    throw new InvalidOperationException("Service is not ready yet.");
                }
                return AlBiruni;
            });

            var resilientFlow = flakyFlow.WithRetry(3);

            // Act
            var outcome = await FlowEngine.ExecuteAsync(resilientFlow);

            // Assert
            Assert.Equal(3, attempts);
            Assert.Equal(Success(AlBiruni), outcome);
        }

        [Fact]
        public async Task WithRetry_WhenAllAttemptsFail_ReturnsLastFailure()
        {
            // Arrange
            var attempts = 0;
            var lastException = new InvalidOperationException("Final failure");
            var flakyFlow = Flow.Create((Func<string>)(() =>
            {
                attempts++;
                throw (attempts == 3) ? lastException : new InvalidOperationException("Intermediate failure");
            }));

            var resilientFlow = flakyFlow.WithRetry(3);

            // Act
            var outcome = await FlowEngine.ExecuteAsync(resilientFlow);

            // Assert
            Assert.Equal(3, attempts);
            Assert.Equal(Failure<string>(lastException), outcome);
        }

        

        [Fact]
        public async Task WithRetry_WhenNonRetryableExceptionThrown_FailsImmediately()
        {
            // Arrange
            var attempts = 0;
            var customException = new InvalidOperationException("Non-retryable exception");
            var flow = Flow.Create((Func<string>)(() =>
            {
                attempts++;
                throw customException;
            }));

            // Configure WithRetry to treat InvalidOperationException as non-retryable
            var resilientFlow = flow.WithRetry(3, typeof(InvalidOperationException));

            // Act
            var outcome = await FlowEngine.ExecuteAsync(resilientFlow);

            // Assert
            Assert.Equal(1, attempts); // Should only attempt once
            Assert.Equal(Failure<string>(customException), outcome);
        }
    }
}
