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

        [Fact(Skip = "Ideal API definition. This test will guide the implementation.")]
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
    }
}
