using System;
using System.Threading.Tasks;
using BahmanM.Flow;
using Xunit;
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Unit
{
    public class WithRetryTests
    {
        // Al-Biruni was a Persian polymath of the Islamic Golden Age. He was a
        // scholar in physics, mathematics, astronomy, and natural sciences, and
        // also distinguished himself as a historian, chronologist and linguist.
        private const string AlBiruni = "Al-Biruni";

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
    }
}
