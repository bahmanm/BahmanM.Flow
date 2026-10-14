namespace BahmanM.Flow.Description.Tests.Unit.Support;

internal static class DescriptionFlowBuilder
{
    internal static Description.IDescription<T> Succeed<T>(T value) => new Description.Primitive.Succeed<T>(value);

    internal static Description.IDescription<T> Fail<T>(Exception exception) => new Description.Primitive.Fail<T>(exception);

    internal static Description.IDescription<T> Create<T>(Flow.Operations.Create.Sync<T> operation) =>
        new Description.Create.Sync<T>(operation);

    internal static Description.IDescription<TOut> Select<TIn, TOut>(
        Description.IDescription<TIn> upstream,
        Flow.Operations.Select.Sync<TIn, TOut> operation) =>
        new Description.Select.Sync<TIn, TOut>(upstream, operation);
}
