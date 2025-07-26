using BahmanM.Flow.Ast.Primitive;

namespace BahmanM.Flow;

public static class Flow
{
    public static IFlow<T> Succeed<T>(T value) => new Ast.Pure.Succeed<T>(value);

    public static IFlow<T> Fail<T>(Exception exception) => new Ast.Pure.Fail<T>(exception);

    public static IFlow<T> Create<T>(Func<T> operation) => new Ast.Create.Sync<T>(() => operation());

    public static IFlow<T> Create<T>(Func<Task<T>> operation) => new Ast.Create.Async<T>(() => operation());

    public static IFlow<T[]> All<T>(IFlow<T> flow, params IFlow<T>[] moreFlows) =>
        new All<T>([flow, ..moreFlows]);

    public static IFlow<T> Any<T>(IFlow<T> flow, params IFlow<T>[] moreFlows) =>
        new Any<T>([flow, ..moreFlows]);
}
