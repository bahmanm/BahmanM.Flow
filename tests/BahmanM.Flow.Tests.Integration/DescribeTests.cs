using System;
using System.Threading.Tasks;
using BahmanM.Flow;
using BahmanM.Flow.Diagnostics;
using Xunit;

namespace BahmanM.Flow.Tests.Integration;

public class DescribeTests
{
    [Fact]
    public void Describe_Succeed_Prints_Primitive_Node()
    {
        // Arrange
        var flow = Flow.Succeed(42);

        // Act
        var plan = flow.DescribeToString();

        // Assert (golden)
        var expected = "Succeed<Int32>\n";
        Assert.Equal(expected, plan);
    }

    [Fact]
    public void Describe_Select_Then_Chain_Prints_Nested_Upstream_Shape()
    {
        // Arrange
        var flow = Flow
            .Succeed(1)
            .Select(i => i + 1)         // Select.Sync<Int32, Int32>
            .Chain(i => Flow.Create(() => i.ToString())); // Chain.Sync<Int32, String>

        // Act
        var plan = flow.DescribeToString();

        // Assert (golden)
        var expected = string.Join("\n", new[]
        {
            "Chain.Sync<Int32, String>",
            "  Select.Sync<Int32, Int32>",
            "    Succeed<Int32>",
            ""
        });
        Assert.Equal(expected, plan);
    }

    [Fact(Skip = "To be completed after core visitor is in place")]
    public void Describe_All_Prints_Indexed_Children()
    {
        var flow = Flow.All(Flow.Succeed("a"), Flow.Succeed("b"));
        var plan = flow.DescribeToString();
        Assert.Contains("All<String>", plan);
    }

    [Fact(Skip = "To be completed after resource rendering is in place")]
    public void Describe_WithResource_Prints_Variant()
    {
        var flow = Flow.WithResource(
            acquire: () => new Dummy(),
            use: _ => Flow.Succeed(1));

        var plan = flow.DescribeToString();
        Assert.Contains("Resource.WithResource<Dummy, Int32>", plan);
    }

    private sealed class Dummy : IDisposable
    {
        public void Dispose() { }
    }
}
