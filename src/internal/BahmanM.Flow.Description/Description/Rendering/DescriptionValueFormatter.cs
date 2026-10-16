using System.Globalization;

namespace BahmanM.Flow.Description.Rendering;

internal static class DescriptionValueFormatter
{
    internal static string Format(object? value, Type declaredType) => value switch
    {
        null => "null",
        string text => $"\"{Escape(text)}\"",
        char character => $"'{Escape(character.ToString())}'",
        bool boolean => boolean ? "true" : "false",
        byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal =>
            ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture),
        Enum enumeration => enumeration.ToString(),
        _ => $"<{TypeFormatter.Format(declaredType)}>"
    };

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal)
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal)
        .Replace("\t", "\\t", StringComparison.Ordinal);
}
