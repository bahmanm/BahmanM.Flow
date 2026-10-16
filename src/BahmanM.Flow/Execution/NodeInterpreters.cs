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
    internal ResourceNodes Resource { get; }

    public NodeInterpreters(Ast.IInterpreter interpreter, Options options)
    {
        Select = new(interpreter, options);
        Chain = new(interpreter, options);
        Primitives = new(interpreter, options);
        Create = new(interpreter, options);
        DoOnSuccess = new(interpreter, options);
        DoOnFailure = new(interpreter, options);
        Validate = new(interpreter, options);
        Resource = new(interpreter, options);
    }

    internal record SelectNodes
    {
        internal SelectNodes(Ast.IInterpreter interpreter, Options options)
        {
            Sync = new Select.Sync(interpreter, options);
            Async = new Select.Async(interpreter, options);
            CancellableAsync = new Select.CancellableAsync(interpreter, options);
        }

        internal Select.Sync Sync { get; }
        internal Select.Async Async { get; }
        internal Select.CancellableAsync CancellableAsync { get; }
    }

    internal record ChainNodes
    {
        internal ChainNodes(Ast.IInterpreter interpreter, Options options)
        {
            Sync = new Chain.Sync(interpreter, options);
            Async = new Chain.Async(interpreter, options);
            CancellableAsync = new Chain.CancellableAsync(interpreter, options);
        }

        internal Chain.Sync Sync { get; }
        internal Chain.Async Async { get; }
        internal Chain.CancellableAsync CancellableAsync { get; }
    }

    internal record PrimitiveNodes
    {
        internal PrimitiveNodes(Ast.IInterpreter interpreter, Options options)
        {
            Any = new Primitive.Any(interpreter, options);
            All = new Primitive.All(interpreter, options);
            Succeed = new Primitive.Succeed(interpreter, options);
            Fail = new Primitive.Fail(interpreter, options);
        }

        internal Primitive.Any Any { get; }
        internal Primitive.All All { get; }
        internal Primitive.Succeed Succeed { get; }
        internal Primitive.Fail Fail { get; }
    }

    internal record CreateNodes
    {
        internal CreateNodes(Ast.IInterpreter interpreter, Options options)
        {
            Sync = new Create.Sync(interpreter, options);
            Async = new Create.Async(interpreter, options);
            CancellableAsync = new Create.CancellableAsync(interpreter, options);
        }

        internal Create.Sync Sync { get; }
        internal Create.Async Async { get; }
        internal Create.CancellableAsync CancellableAsync { get; }
    }

    internal record DoOnSuccessNodes
    {
        internal DoOnSuccessNodes(Ast.IInterpreter interpreter, Options options)
        {
            Sync = new DoOnSuccess.Sync(interpreter, options);
            Async = new DoOnSuccess.Async(interpreter, options);
            CancellableAsync = new DoOnSuccess.CancellableAsync(interpreter, options);
        }

        internal DoOnSuccess.Sync Sync { get; }
        internal DoOnSuccess.Async Async { get; }
        internal DoOnSuccess.CancellableAsync CancellableAsync { get; }
    }

    internal record DoOnFailureNodes
    {
        internal DoOnFailureNodes(Ast.IInterpreter interpreter, Options options)
        {
            Sync = new DoOnFailure.Sync(interpreter, options);
            Async = new DoOnFailure.Async(interpreter, options);
            CancellableAsync = new DoOnFailure.CancellableAsync(interpreter, options);
        }

        internal DoOnFailure.Sync Sync { get; }
        internal DoOnFailure.Async Async { get; }
        internal DoOnFailure.CancellableAsync CancellableAsync { get; }
    }

    internal record ValidateNodes
    {
        internal ValidateNodes(Ast.IInterpreter interpreter, Options options)
        {
            Sync = new Validate.Sync(interpreter, options);
            Async = new Validate.Async(interpreter, options);
            CancellableAsync = new Validate.CancellableAsync(interpreter, options);
        }

        internal Validate.Sync Sync { get; }
        internal Validate.Async Async { get; }
        internal Validate.CancellableAsync CancellableAsync { get; }
    }

    internal record ResourceNodes
    {
        internal ResourceNodes(Ast.IInterpreter interpreter, Options options)
        {
            WithResource = new Resource.WithResource(interpreter, options);
            WithResourceAsync = new Resource.WithResourceAsyncExecutor(interpreter, options);
            WithResourceCancellableAsync = new Resource.WithResourceCancellableAsyncExecutor(interpreter, options);
        }

        internal Resource.WithResource WithResource { get; }
        internal Resource.WithResourceAsyncExecutor WithResourceAsync { get; }
        internal Resource.WithResourceCancellableAsyncExecutor WithResourceCancellableAsync { get; }
    }
}
