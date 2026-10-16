using System.Runtime.CompilerServices;
using BahmanM.Flow.Description.Rendering;

namespace BahmanM.Flow.Description.Rendering.Projection;

internal static class OperationFormatter
{
    internal static string Format(Delegate operation)
    {
        var invoke = operation.GetType().GetMethod("Invoke")!;
        var inputs = string.Join(", ", invoke.GetParameters().Select(parameter => TypeFormatter.Format(parameter.ParameterType)));
        var method = operation.Method;
        var isLambda = method.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) || method.Name.Contains('<');
        var identity = isLambda
            ? "lambda"
            : $"{method.DeclaringType?.FullName ?? "<global>"}.{method.Name}";

        return $"{identity}({inputs}) -> {TypeFormatter.Format(invoke.ReturnType)}";
    }
}
