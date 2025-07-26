namespace BahmanM.Flow;

public static class Flow
{
    public static IFlow<T> Succeed<T>(T value) => new SucceededNode<T>(value);
    
    public static IFlow<T> Fail<T>(Exception exception) => new FailedNode<T>(exception);
    
    public static IFlow<T> Create<T>(Func<T> operation) => new CreateNode<T>(operation);
    
    public static IFlow<T> Create<T>(Func<Task<T>> operation) => new AsyncCreateNode<T>(operation);

    public static IFlow<T[]> All<T>(IFlow<T> flow, params IFlow<T>[] moreFlows) =>
        new AllNode<T>([flow, ..moreFlows]);

    public static IFlow<T> Any<T>(IFlow<T> flow, params IFlow<T>[] moreFlows) =>
        new AnyNode<T>([flow, ..moreFlows]);
}
