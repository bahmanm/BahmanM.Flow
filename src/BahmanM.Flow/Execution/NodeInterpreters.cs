namespace BahmanM.Flow.Execution;

internal class NodeInterpreters
{
    internal SelectNodes Select { get; }
    internal ChainNodes Chain { get; }
    internal PrimitiveNodes Primitives { get; }
    internal CreateNodes Create { get; }
    internal DoOnSuccessNodes DoOnSuccess { get; }
    internal DoOnFailureNodes DoOnFailure { get; }
    internal ValidateNodes Validate { get; }

    public NodeInterpreters(Ast.IInterpreter interpreter, Options options)
    {
        Select = new(interpreter, options);
        Chain = new(interpreter, options);
        Primitives = new(interpreter, options);
        Create = new(interpreter, options);
        DoOnSuccess = new(interpreter, options);
        DoOnFailure = new(interpreter, options);
        Validate = new(interpreter, options);
    }

    internal record SelectNodes
    {
        internal SelectNodes(Ast.IInterpreter interpreter, Options options)
        {
            Sync = new Execution.Select.Sync(interpreter, options);
            Async = new Execution.Select.Async(interpreter, options);
            CancellableAsync = new Execution.Select.CancellableAsync(interpreter, options);
        }

        internal Execution.Select.Sync Sync { get; }
        internal Execution.Select.Async Async { get; }
        internal Execution.Select.CancellableAsync CancellableAsync { get; }
    }

    internal record ChainNodes
    {
        internal ChainNodes(Ast.IInterpreter interpreter, Options options)
        {
            Sync = new Execution.Chain.Sync(interpreter, options);
            Async = new Execution.Chain.Async(interpreter, options);
            CancellableAsync = new Execution.Chain.CancellableAsync(interpreter, options);
        }

        internal Execution.Chain.Sync Sync { get; }
        internal Execution.Chain.Async Async { get; }
        internal Execution.Chain.CancellableAsync CancellableAsync { get; }
    }

    internal record PrimitiveNodes
    {
        internal PrimitiveNodes(Ast.IInterpreter interpreter, Options options)
        {
            Any = new Execution.Primitive.Any(interpreter, options);
            All = new Execution.Primitive.All(interpreter, options);
            Succeed = new Execution.Primitive.Succeed(interpreter, options);
            Fail = new Execution.Primitive.Fail(interpreter, options);
        }

        internal Execution.Primitive.Any Any { get; }
        internal Execution.Primitive.All All { get; }
        internal Execution.Primitive.Succeed Succeed { get; }
        internal Execution.Primitive.Fail Fail { get; }
    }

    internal record CreateNodes
    {
        internal CreateNodes(Ast.IInterpreter interpreter, Options options)
        {
            Sync = new Execution.Create.Sync(interpreter, options);
            Async = new Execution.Create.Async(interpreter, options);
            CancellableAsync = new Execution.Create.CancellableAsync(interpreter, options);
        }

        internal Execution.Create.Sync Sync { get; }
        internal Execution.Create.Async Async { get; }
        internal Execution.Create.CancellableAsync CancellableAsync { get; }
    }

    internal record DoOnSuccessNodes
    {
        internal DoOnSuccessNodes(Ast.IInterpreter interpreter, Options options)
        {
            Sync = new Execution.DoOnSuccess.Sync(interpreter, options);
            Async = new Execution.DoOnSuccess.Async(interpreter, options);
            CancellableAsync = new Execution.DoOnSuccess.CancellableAsync(interpreter, options);
        }

        internal Execution.DoOnSuccess.Sync Sync { get; }
        internal Execution.DoOnSuccess.Async Async { get; }
        internal Execution.DoOnSuccess.CancellableAsync CancellableAsync { get; }
    }

    internal record DoOnFailureNodes
    {
        internal DoOnFailureNodes(Ast.IInterpreter interpreter, Options options)
        {
            Sync = new Execution.DoOnFailure.Sync(interpreter, options);
            Async = new Execution.DoOnFailure.Async(interpreter, options);
            CancellableAsync = new Execution.DoOnFailure.CancellableAsync(interpreter, options);
        }

        internal Execution.DoOnFailure.Sync Sync { get; }
        internal Execution.DoOnFailure.Async Async { get; }
        internal Execution.DoOnFailure.CancellableAsync CancellableAsync { get; }
    }

    internal record ValidateNodes
    {
        internal ValidateNodes(Ast.IInterpreter interpreter, Options options)
        {
            Sync = new Execution.Validate.Sync(interpreter, options);
            Async = new Execution.Validate.Async(interpreter, options);
            CancellableAsync = new Execution.Validate.CancellableAsync(interpreter, options);
        }

        internal Execution.Validate.Sync Sync { get; }
        internal Execution.Validate.Async Async { get; }
        internal Execution.Validate.CancellableAsync CancellableAsync { get; }
    }
}

internal static class FlowExtensions
{
    internal static Ast.INode<T> AsNode<T>(this IFlow<T> flow) => (Ast.INode<T>)flow;
}
