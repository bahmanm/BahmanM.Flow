namespace BahmanM.Flow.Description.Serialisation;

internal sealed class DescriptionViewProjector : IDescriptionVisitor<DescriptionView>
{
    public DescriptionView Visit<T>(Primitive.Succeed<T> node) =>
        Create("Succeed", node.Id, [typeof(T)], [
            new DescriptionField("value", DescriptionValueFormatter.Format(node.Value, typeof(T)))
        ]);

    public DescriptionView Visit<T>(Primitive.Fail<T> node) =>
        Create("Fail", node.Id, [typeof(T)], [
            new DescriptionField("exception", TypeFormatter.Format(node.Exception.GetType()))
        ]);

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

    public DescriptionView Visit<T>(DoOnSuccess.Sync<T> node) =>
        Create("DoOnSuccess.Sync", node.Id, [typeof(T)], [
            new DescriptionField("action", OperationFormatter.Format(node.Action))
        ], node.Upstream);

    public DescriptionView Visit<T>(DoOnFailure.Sync<T> node) =>
        Create("DoOnFailure.Sync", node.Id, [typeof(T)], [
            new DescriptionField("action", OperationFormatter.Format(node.Action))
        ], node.Upstream);

    private DescriptionView Create(
        string operatorName,
        string id,
        IReadOnlyList<Type> typeArguments,
        IReadOnlyList<DescriptionField> fields,
        IDescription? upstream = null) =>
        new(
            operatorName,
            id,
            typeArguments,
            fields,
            upstream?.Accept(this));
}
