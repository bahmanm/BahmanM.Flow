namespace BahmanM.Flow;

public class FlowEngine<T>
{
    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow)
    {
        var node = (Ast.INode<T>)flow;
        return node.Accept(new FlowEngine<T>().Interpreter);
    }

    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow, Execution.Options options)
    {
        var node = (Ast.INode<T>)flow;
        return node.Accept(new FlowEngine<T>(options).Interpreter);
    }

    private Execution.Options Options { get; }
    private Execution.Interpreter<T> Interpreter { get; }

    private FlowEngine() : this(new Execution.Interpreter<T>(new Execution.Options(CancellationToken.None)), new Execution.Options(CancellationToken.None))
    {
    }

    private FlowEngine(Execution.Options options) : this(new Execution.Interpreter<T>(options), options)
    {
    }

    private FlowEngine(Execution.Interpreter<T> interpreter, Execution.Options options)
    {
        Options = options;
        Interpreter = interpreter;
    }
}
