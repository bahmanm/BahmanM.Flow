namespace BahmanM.Flow;

public class FlowEngine
{
    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow)
    {
        var node = (Ast.INode<T>)flow;
        return node.Accept(new FlowEngine().Interpreter);
    }

    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow, Execution.Options options)
    {
        var node = (Ast.INode<T>)flow;
        return node.Accept(new FlowEngine(options).Interpreter);
    }

    private Execution.Options Options { get; }
    private Ast.IInterpreter Interpreter { get; }

    private FlowEngine() : this(new Execution.Interpreter(new Execution.Options(CancellationToken.None)), new Execution.Options(CancellationToken.None))
    {
    }

    private FlowEngine(Execution.Options options) : this(new Execution.Interpreter(options), options)
    {
    }

    private FlowEngine(Ast.IInterpreter interpreter, Execution.Options options)
    {
        Options = options;
        Interpreter = interpreter;
    }
}
