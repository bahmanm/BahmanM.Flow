namespace BahmanM.Flow.Description.Serialisation;

internal sealed class DescriptionViewProjector : IDescriptionVisitor<DescriptionView>
{
    public DescriptionView Visit<T>(ExistingFlow<T> node) =>
        Create("ExistingFlow", node.Id, [typeof(T)], [
            new DescriptionField("runtimeType", TypeFormatter.Format(node.Flow.GetType()))
        ]);

    public DescriptionView Visit<T>(Primitive.Succeed<T> node) =>
        Create("Succeed", node.Id, [typeof(T)], [
            new DescriptionField("value", DescriptionValueFormatter.Format(node.Value, typeof(T)))
        ]);

    public DescriptionView Visit<T>(Primitive.Fail<T> node) =>
        Create("Fail", node.Id, [typeof(T)], [
            new DescriptionField("exception", TypeFormatter.Format(node.Exception.GetType()))
        ]);

    public DescriptionView Visit<T>(Primitive.All<T> node) =>
        Create("All", node.Id, [typeof(T)], [], flows: node.Flows);

    public DescriptionView Visit<T>(Primitive.Any<T> node) =>
        Create("Any", node.Id, [typeof(T)], [], flows: node.Flows);

    public DescriptionView Visit<T>(Create.Sync<T> node) =>
        Create("Create.Sync", node.Id, [typeof(T)], [
            new DescriptionField("operation", OperationFormatter.Format(node.Operation))
        ]);

    public DescriptionView Visit<T>(Create.Async<T> node) =>
        Create("Create.Async", node.Id, [typeof(T)], [
            new DescriptionField("operation", OperationFormatter.Format(node.Operation))
        ]);

    public DescriptionView Visit<T>(Create.CancellableAsync<T> node) =>
        Create("Create.CancellableAsync", node.Id, [typeof(T)], [
            new DescriptionField("operation", OperationFormatter.Format(node.Operation))
        ]);

    public DescriptionView Visit<T>(Behaviour.Retry.Retry<T> node) =>
        Create("Retry", node.Id, [typeof(T)], [
            new DescriptionField("maxAttempts", node.MaxAttempts.ToString()),
            new DescriptionField("nonRetryableExceptions", string.Join(", ", node.NonRetryableExceptions.Select(TypeFormatter.Format)))
        ], node.Source);

    public DescriptionView Visit<T>(Behaviour.Timeout.Timeout<T> node) =>
        Create("Timeout", node.Id, [typeof(T)], [
            new DescriptionField("duration", node.Duration.ToString("c", System.Globalization.CultureInfo.InvariantCulture))
        ], node.Source);

    public DescriptionView Visit<T>(Behaviour.Custom.Custom<T> node) =>
        Create("CustomBehaviour", node.Id, [typeof(T)], [
            new DescriptionField("operationType", node.Behaviour.OperationType)
        ], children: node.CachedResultDescription is { } cachedResultDescription
            ? [
                new DescriptionChild("source", node.Source.Accept(this)),
                new DescriptionChild("cachedResultDescription", cachedResultDescription.Accept(this))
            ]
            : [new DescriptionChild("source", node.Source.Accept(this))]);

    public DescriptionView Visit<TIn, TOut>(Select.Sync<TIn, TOut> node) =>
        Create("Select.Sync", node.Id, [typeof(TIn), typeof(TOut)], [
            new DescriptionField("operation", OperationFormatter.Format(node.Operation))
        ], node.Upstream);

    public DescriptionView Visit<TIn, TOut>(Select.Async<TIn, TOut> node) =>
        Create("Select.Async", node.Id, [typeof(TIn), typeof(TOut)], [
            new DescriptionField("operation", OperationFormatter.Format(node.Operation))
        ], node.Upstream);

    public DescriptionView Visit<TIn, TOut>(Select.CancellableAsync<TIn, TOut> node) =>
        Create("Select.CancellableAsync", node.Id, [typeof(TIn), typeof(TOut)], [
            new DescriptionField("operation", OperationFormatter.Format(node.Operation))
        ], node.Upstream);

    public DescriptionView Visit<T>(Validate.Sync<T> node) =>
        Create("Validate.Sync", node.Id, [typeof(T)], [
            new DescriptionField("predicate", OperationFormatter.Format(node.Predicate)),
            new DescriptionField("exceptionFactory", OperationFormatter.Format(node.ExceptionFactory))
        ], node.Upstream);

    public DescriptionView Visit<T>(Validate.Async<T> node) =>
        Create("Validate.Async", node.Id, [typeof(T)], [
            new DescriptionField("predicate", OperationFormatter.Format(node.PredicateAsync)),
            new DescriptionField("exceptionFactory", OperationFormatter.Format(node.ExceptionFactory))
        ], node.Upstream);

    public DescriptionView Visit<T>(Validate.CancellableAsync<T> node) =>
        Create("Validate.CancellableAsync", node.Id, [typeof(T)], [
            new DescriptionField("predicate", OperationFormatter.Format(node.PredicateCancellableAsync)),
            new DescriptionField("exceptionFactory", OperationFormatter.Format(node.ExceptionFactory))
        ], node.Upstream);

    public DescriptionView Visit<T>(DoOnSuccess.Sync<T> node) =>
        Create("DoOnSuccess.Sync", node.Id, [typeof(T)], [
            new DescriptionField("action", OperationFormatter.Format(node.Action))
        ], node.Upstream);

    public DescriptionView Visit<T>(DoOnSuccess.Async<T> node) =>
        Create("DoOnSuccess.Async", node.Id, [typeof(T)], [
            new DescriptionField("action", OperationFormatter.Format(node.AsyncAction))
        ], node.Upstream);

    public DescriptionView Visit<T>(DoOnSuccess.CancellableAsync<T> node) =>
        Create("DoOnSuccess.CancellableAsync", node.Id, [typeof(T)], [
            new DescriptionField("action", OperationFormatter.Format(node.AsyncAction))
        ], node.Upstream);

    public DescriptionView Visit<T>(DoOnFailure.Sync<T> node) =>
        Create("DoOnFailure.Sync", node.Id, [typeof(T)], [
            new DescriptionField("action", OperationFormatter.Format(node.Action))
        ], node.Upstream);

    public DescriptionView Visit<T>(DoOnFailure.Async<T> node) =>
        Create("DoOnFailure.Async", node.Id, [typeof(T)], [
            new DescriptionField("action", OperationFormatter.Format(node.AsyncAction))
        ], node.Upstream);

    public DescriptionView Visit<T>(DoOnFailure.CancellableAsync<T> node) =>
        Create("DoOnFailure.CancellableAsync", node.Id, [typeof(T)], [
            new DescriptionField("action", OperationFormatter.Format(node.AsyncAction))
        ], node.Upstream);

    public DescriptionView Visit<TIn, TOut>(Chain.Sync<TIn, TOut> node) =>
        Create("Chain.Sync", node.Id, [typeof(TIn), typeof(TOut)], [
            new DescriptionField("operation", OperationFormatter.Format(node.Operation))
        ], node.Upstream);

    public DescriptionView Visit<TIn, TOut>(Chain.Async<TIn, TOut> node) =>
        Create("Chain.Async", node.Id, [typeof(TIn), typeof(TOut)], [
            new DescriptionField("operation", OperationFormatter.Format(node.Operation))
        ], node.Upstream);

    public DescriptionView Visit<TIn, TOut>(Chain.CancellableAsync<TIn, TOut> node) =>
        Create("Chain.CancellableAsync", node.Id, [typeof(TIn), typeof(TOut)], [
            new DescriptionField("operation", OperationFormatter.Format(node.Operation))
        ], node.Upstream);

    public DescriptionView Visit<T>(Recover.Sync<T> node) =>
        Create("Recover.Sync", node.Id, [typeof(T)], [
            new DescriptionField("recover", OperationFormatter.Format(node.Recover))
        ], node.Source);

    public DescriptionView Visit<T>(Recover.Async<T> node) =>
        Create("Recover.Async", node.Id, [typeof(T)], [
            new DescriptionField("recover", OperationFormatter.Format(node.Recover))
        ], node.Source);

    public DescriptionView Visit<T>(Recover.CancellableAsync<T> node) =>
        Create("Recover.CancellableAsync", node.Id, [typeof(T)], [
            new DescriptionField("recover", OperationFormatter.Format(node.Recover))
        ], node.Source);

    public DescriptionView Visit<TResource, T>(Resource.WithResource<TResource, T> node)
        where TResource : IDisposable =>
        Create("WithResource.Sync", node.Id, [typeof(TResource), typeof(T)], [
            new DescriptionField("acquire", OperationFormatter.Format(node.Acquire)),
            new DescriptionField("use", OperationFormatter.Format(node.Use))
        ]);

    public DescriptionView Visit<TResource, T>(Resource.WithResourceAsync<TResource, T> node)
        where TResource : IAsyncDisposable =>
        Create("WithResource.Async", node.Id, [typeof(TResource), typeof(T)], [
            new DescriptionField("acquire", OperationFormatter.Format(node.AcquireAsync)),
            new DescriptionField("use", OperationFormatter.Format(node.Use))
        ]);

    public DescriptionView Visit<TResource, T>(Resource.WithResourceCancellableAsync<TResource, T> node)
        where TResource : IAsyncDisposable =>
        Create("WithResource.CancellableAsync", node.Id, [typeof(TResource), typeof(T)], [
            new DescriptionField("acquire", OperationFormatter.Format(node.AcquireAsync)),
            new DescriptionField("use", OperationFormatter.Format(node.Use))
        ]);

    private DescriptionView Create(
        string operatorName,
        string id,
        IReadOnlyList<Type> typeArguments,
        IReadOnlyList<DescriptionField> fields,
        IDescription? upstream = null,
        IReadOnlyList<IDescription>? flows = null,
        IReadOnlyList<DescriptionChild>? children = null) =>
        new(
            operatorName,
            id,
            typeArguments,
            fields,
            children ?? (upstream is not null
                ? [new DescriptionChild("upstream", upstream.Accept(this))]
                : flows?.Select((flow, index) => new DescriptionChild($"flow[{index}]", flow.Accept(this))).ToArray() ?? []));
}
