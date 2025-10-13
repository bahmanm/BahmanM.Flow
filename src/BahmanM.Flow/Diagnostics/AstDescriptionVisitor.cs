using System;
using System.Reflection;
using BahmanM.Flow.Ast;
using BahmanM.Flow.Support;

namespace BahmanM.Flow.Diagnostics;

internal sealed class AstDescriptionVisitor
{
    private readonly IndentWriter _writer;
    private readonly System.Collections.Generic.HashSet<object> _seen = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
    private const int MaxDepth = 128;

    public AstDescriptionVisitor(IndentWriter writer)
    {
        _writer = writer;
    }

    public void Visit<T>(INode<T> node)
    {
        VisitNode(node);
    }

    private void VisitNode(object nodeObj, int depth = 0)
    {
        if (depth >= MaxDepth)
        {
            _writer.WriteLine("… (max depth reached)");
            return;
        }
        if (!_seen.Add(nodeObj))
        {
            _writer.WriteLine("(cycle)");
            return;
        }
        var nodeType = nodeObj.GetType();
        var (label, typeArgs) = Classify(nodeType);
        _writer.WriteLine(FormatLabel(label, typeArgs));

        // Traverse typical single-upstream edges without executing user code.
        // Many AST records expose either Upstream or Source.
        var upstreamProp = nodeType.GetProperty("Upstream", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (upstreamProp != null)
        {
            var upstreamFlow = upstreamProp.GetValue(nodeObj);
            if (upstreamFlow is not null)
            {
                var valueType = ExtractIFlowValueType(upstreamFlow.GetType());
                if (valueType is not null)
                {
                    _writer.Indent();
                    VisitNode(AsNodeViaReflection(valueType, upstreamFlow), depth + 1);
                    _writer.Unindent();
                }
            }
            return; // Prefer single-chain rendering for clarity
        }

        var sourceProp = nodeType.GetProperty("Source", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (sourceProp != null)
        {
            var sourceFlow = sourceProp.GetValue(nodeObj);
            if (sourceFlow is not null)
            {
                var valueType = ExtractIFlowValueType(sourceFlow.GetType());
                if (valueType is not null)
                {
                    _writer.Indent();
                    VisitNode(AsNodeViaReflection(valueType, sourceFlow), depth + 1);
                    _writer.Unindent();
                }
            }
            return;
        }

        // Collections (All/Any): enumerate Flows and render indexed children.
        var flowsProp = nodeType.GetProperty("Flows", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (flowsProp != null)
        {
            if (flowsProp.GetValue(nodeObj) is System.Collections.IEnumerable enumerable)
            {
                var idx = 0;
                foreach (var childFlow in enumerable)
                {
                    var childValueType = childFlow is not null ? ExtractIFlowValueType(childFlow.GetType()) : null;
                    if (childValueType is not null)
                    {
                        _writer.Indent();
                        _writer.WriteLine($"[{idx++}]");
                        _writer.Indent();
                        VisitNode(AsNodeViaReflection(childValueType, childFlow!), depth + 1);
                        _writer.Unindent();
                        _writer.Unindent();
                    }
                }
            }
        }
    }

    private static string FormatLabel(string label, Type[] typeArgs)
    {
        if (typeArgs.Length == 0) return label;
        return $"{label}<{string.Join(", ", Array.ConvertAll(typeArgs, ShortType))}>";
    }

    private static string ShortType(Type t)
    {
        // Keep it simple and stable: use Name for primitives and common types.
        if (t.IsArray)
            return $"{ShortType(t.GetElementType()!)}[]";
        if (t.IsGenericType)
            return t.Name.Split('`')[0];
        return t.Name;
    }

    private static (string label, Type[] typeArgs) Classify(Type nodeType)
    {
        if (!nodeType.IsGenericType) return (nodeType.Name, Type.EmptyTypes);
        var def = nodeType.GetGenericTypeDefinition();
        var args = nodeType.GetGenericArguments();

        // Primitive
        if (def == typeof(BahmanM.Flow.Ast.Primitive.Succeed<>)) return ("Succeed", [args[0]]);
        if (def == typeof(BahmanM.Flow.Ast.Primitive.Fail<>)) return ("Fail", [args[0]]);
        if (def == typeof(BahmanM.Flow.Ast.Primitive.All<>)) return ("All", [args[0]]); // element type
        if (def == typeof(BahmanM.Flow.Ast.Primitive.Any<>)) return ("Any", [args[0]]);

        // Create
        if (def == typeof(BahmanM.Flow.Ast.Create.Sync<>)) return ("Create.Sync", [args[0]]);
        if (def == typeof(BahmanM.Flow.Ast.Create.Async<>)) return ("Create.Async", [args[0]]);
        if (def == typeof(BahmanM.Flow.Ast.Create.CancellableAsync<>)) return ("Create.CancellableAsync", [args[0]]);

        // Select
        if (def == typeof(BahmanM.Flow.Ast.Select.Sync<,>)) return ("Select.Sync", [args[0], args[1]]);
        if (def == typeof(BahmanM.Flow.Ast.Select.Async<,>)) return ("Select.Async", [args[0], args[1]]);
        if (def == typeof(BahmanM.Flow.Ast.Select.CancellableAsync<,>)) return ("Select.CancellableAsync", [args[0], args[1]]);

        // Chain
        if (def == typeof(BahmanM.Flow.Ast.Chain.Sync<,>)) return ("Chain.Sync", [args[0], args[1]]);
        if (def == typeof(BahmanM.Flow.Ast.Chain.Async<,>)) return ("Chain.Async", [args[0], args[1]]);
        if (def == typeof(BahmanM.Flow.Ast.Chain.CancellableAsync<,>)) return ("Chain.CancellableAsync", [args[0], args[1]]);

        // Recover
        if (def == typeof(BahmanM.Flow.Ast.Recover.Sync<>)) return ("Recover.Sync", [args[0]]);
        if (def == typeof(BahmanM.Flow.Ast.Recover.Async<>)) return ("Recover.Async", [args[0]]);
        if (def == typeof(BahmanM.Flow.Ast.Recover.CancellableAsync<>)) return ("Recover.CancellableAsync", [args[0]]);

        // Validate
        if (def == typeof(BahmanM.Flow.Ast.Validate.Sync<>)) return ("Validate.Sync", [args[0]]);
        if (def == typeof(BahmanM.Flow.Ast.Validate.Async<>)) return ("Validate.Async", [args[0]]);
        if (def == typeof(BahmanM.Flow.Ast.Validate.CancellableAsync<>)) return ("Validate.CancellableAsync", [args[0]]);

        // DoOnSuccess
        if (def == typeof(BahmanM.Flow.Ast.DoOnSuccess.Sync<>)) return ("DoOnSuccess.Sync", [args[0]]);
        if (def == typeof(BahmanM.Flow.Ast.DoOnSuccess.Async<>)) return ("DoOnSuccess.Async", [args[0]]);
        if (def == typeof(BahmanM.Flow.Ast.DoOnSuccess.CancellableAsync<>)) return ("DoOnSuccess.CancellableAsync", [args[0]]);

        // DoOnFailure
        if (def == typeof(BahmanM.Flow.Ast.DoOnFailure.Sync<>)) return ("DoOnFailure.Sync", [args[0]]);
        if (def == typeof(BahmanM.Flow.Ast.DoOnFailure.Async<>)) return ("DoOnFailure.Async", [args[0]]);
        if (def == typeof(BahmanM.Flow.Ast.DoOnFailure.CancellableAsync<>)) return ("DoOnFailure.CancellableAsync", [args[0]]);

        // Resources
        if (def == typeof(BahmanM.Flow.Ast.Resource.WithResource<,>)) return ("Resource.WithResource", [args[0], args[1]]);
        if (def == typeof(BahmanM.Flow.Ast.Resource.WithResourceAsync<,>)) return ("Resource.WithResourceAsync", [args[0], args[1]]);
        if (def == typeof(BahmanM.Flow.Ast.Resource.WithResourceCancellableAsync<,>)) return ("Resource.WithResourceCancellableAsync", [args[0], args[1]]);

        return (nodeType.Name, args);
    }

    private static Type? ExtractIFlowValueType(Type flowType)
    {
        if (flowType.IsGenericType && flowType.GetGenericTypeDefinition() == typeof(IFlow<>))
            return flowType.GetGenericArguments()[0];
        foreach (var iface in flowType.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IFlow<>))
                return iface.GetGenericArguments()[0];
        }
        return null;
    }

    private static object AsNodeViaReflection(Type valueType, object flow)
    {
        var extType = typeof(FlowNodeExtensions);
        var method = extType.GetMethod("AsNode", BindingFlags.Static | BindingFlags.NonPublic);
        var generic = method!.MakeGenericMethod(valueType);
        return generic.Invoke(null, new[] { flow })!;
    }
}
