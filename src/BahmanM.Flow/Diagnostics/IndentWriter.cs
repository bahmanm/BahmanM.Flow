using System.Text;

namespace BahmanM.Flow.Diagnostics;

internal sealed class IndentWriter
{
    private readonly StringBuilder _sb = new();
    private int _level;
    private const string IndentToken = "  "; // two spaces

    public void Indent() => _level++;
    public void Unindent() { if (_level > 0) _level--; }

    public void WriteLine(string text)
    {
        for (var i = 0; i < _level; i++) _sb.Append(IndentToken);
        _sb.Append(text);
        _sb.Append('\n');
    }

    public override string ToString() => _sb.ToString();
}

