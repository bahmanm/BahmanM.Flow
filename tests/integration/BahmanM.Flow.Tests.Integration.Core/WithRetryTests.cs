using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Integration.Core
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
                Add(Flow.Succeed("s").Select<string,string>(async _ => { await Task.Yield(); return "async selected"; }));
                Add(Flow.Succeed("s").DoOnSuccess(_ => { }));
                Add(Flow.Succeed("s").DoOnSuccess(async _ => await Task.Yield()));
                Add(Flow.Succeed("s").DoOnFailure(_ => { }));
                Add(Flow.Succeed("s").DoOnFailure(async _ => await Task.Yield()));
            }
        }


        [Theory]
        [ClassData(typeof(NonFailableFlowsTheoryData))]
        public async Task WithRetry_OnNonFailableNodes_IsANoOpAndPreservesOutcome(IFlow<string> nonFailableFlow)
        {
            // Arrange
            var originalFlow = nonFailableFlow;

            // Act
            var resultFlow = originalFlow.WithRetry(3);

            // Assert
            Assert.Equal(
                await FlowEngine.ExecuteAsync(originalFlow),
                await FlowEngine.ExecuteAsync(resultFlow));
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
        public async Task WithRetry_BeforeSelectRetriesCreate_ButAfterSelectDoesNot()
        {
            var retriesBeforeSelect = 0;
            var selectedBeforeSelect = 0;
            var flowWithRetryBeforeSelect = Flow.Create<string>(() =>
            {
                retriesBeforeSelect++;
                if (retriesBeforeSelect < 3)
                {
                    throw new InvalidOperationException("Temporary failure.");
                }

                return "value";
            })
                .WithRetry(3)
                .Select(value =>
                {
                    selectedBeforeSelect++;
                    return value;
                });

            var attemptsWithRetryAfterSelect = 0;
            var flowWithRetryAfterSelect = Flow.Create<string>((Func<string>)(() =>
            {
                attemptsWithRetryAfterSelect++;
                throw new InvalidOperationException("Temporary failure.");
            }))
                .Select(value => value)
                .WithRetry(3);

            var retriedBeforeSelectOutcome = await FlowEngine.ExecuteAsync(flowWithRetryBeforeSelect);
            var retriedAfterSelectOutcome = await FlowEngine.ExecuteAsync(flowWithRetryAfterSelect);

            Assert.Equal(Success("value"), retriedBeforeSelectOutcome);
            Assert.Equal(3, retriesBeforeSelect);
            Assert.Equal(1, selectedBeforeSelect);

            Assert.IsType<Failure<string>>(retriedAfterSelectOutcome);
            Assert.Equal(1, attemptsWithRetryAfterSelect);
        }

        [Fact]
        public async Task WithRetry_AppliedToSelect_DoesNotRetryFailingSelector()
        {
            var selectorInvocations = 0;
            var exception = new InvalidOperationException("Selector failed.");
            var flow = Flow.Succeed(42)
                .Select((Flow.Operations.Select.Sync<int, int>)(_ =>
                {
                    selectorInvocations++;
                    throw exception;
                }))
                .WithRetry(3);

            var outcome = await FlowEngine.ExecuteAsync(flow);

            Assert.Equal(Failure<int>(exception), outcome);
            Assert.Equal(1, selectorInvocations);
        }

        [Fact]
        public async Task WithRetry_OnFlakyAsyncOperation_SucceedsAfterRetries()
        {
            // Arrange
            var attempts = 0;
            var flakyFlow = Flow.Create<string>(async () =>
            {
                attempts++;
                await Task.Yield();
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
            Func<string> operation = () =>
            {
                attempts++;
                throw (attempts == 3) ? lastException : new InvalidOperationException("Intermediate failure");
            };
            var flakyFlow = Flow.Create(operation);

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
            Func<string> operation = () =>
            {
                attempts++;
                throw customException;
            };
            var flow = Flow.Create(operation);

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
