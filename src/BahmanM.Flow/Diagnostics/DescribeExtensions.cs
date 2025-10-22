using System;
using BahmanM.Flow.Support;

namespace BahmanM.Flow.Diagnostics;

/// <summary>
/// Inspectable Execution Plans: extension methods to describe a flow's AST.
/// </summary>
public static class DescribeExtensions
{
    /// <summary>
    /// Builds a human-readable description of the flow's structure and writes it using the provided writer.
    /// Returns the original flow unchanged.
    /// </summary>
    public static IFlow<T> Describe<T>(this IFlow<T> flow, Action<string> writer)
    {
        if (writer is null) throw new ArgumentNullException(nameof(writer));
        var text = flow.DescribeToString();
        writer(text);
        return flow;
    }

    /// <summary>
    /// Builds and returns a human-readable description of the flow's structure.
    /// </summary>
    public static string DescribeToString<T>(this IFlow<T> flow)
    {
        var node = flow.AsNode();
        var iw = new IndentWriter();
        var visitor = new AstDescriptionVisitor(iw);
        visitor.Visit(node);
        return iw.ToString();
    }
}

