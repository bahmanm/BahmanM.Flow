using System.Security.Cryptography;
using System.Text;
using BahmanM.Flow.Description.Serialisation;

namespace BahmanM.Flow.Description;

internal interface IOperation
{
    Delegate Operation { get; }
}

internal interface IValue
{
    object? Value { get; }
}

internal interface IFailure
{
    Exception Exception { get; }
}

internal interface IDescription<T> : IDescription, IFlow<T>
{
    Ast.INode<T> Lower();
}

internal abstract record Node<T> : IDescription<T>
{
    protected Node(string operatorName)
    {
        OperatorName = operatorName;
        Id = Guid.NewGuid().ToString("N");
        ShortId = CreateShortId(Id);
    }

    public string Id { get; }
    public string ShortId { get; }
    public string OperatorName { get; }
    public virtual string OperatorDisplayName => $"{OperatorName}<{TypeFormatter.Format(ValueType)}>";
    public Type ValueType => typeof(T);
    public virtual IDescription? Source => null;

    public abstract Ast.INode<T> Lower();

    private static string CreateShortId(string id) =>
        Convert.ToHexString(SHA1.HashData(Encoding.ASCII.GetBytes(id)))[..10].ToLowerInvariant();
}
