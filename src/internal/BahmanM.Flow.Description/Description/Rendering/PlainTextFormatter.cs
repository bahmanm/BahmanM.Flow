using System.Security.Cryptography;
using System.Text;

namespace BahmanM.Flow.Description.Rendering;

internal sealed class PlainTextFormatter : IFormatter
{
    public string Format(DescriptionView view)
    {
        var output = new StringBuilder();
        Append(view, output, string.Empty, isRoot: true, includeLabel: true);
        return output.ToString().TrimEnd();
    }

    private static void Append(
        DescriptionView view,
        StringBuilder output,
        string prefix,
        bool isRoot,
        bool includeLabel)
    {
        if (!isRoot && includeLabel)
            output.Append(prefix).Append("└── ");

        if (includeLabel)
            output.Append(FormatOperatorName(view)).Append(" [node: ").Append(ShortId(view.Id)).AppendLine("]");

        var entries = view.Fields
            .Select(field => (Label: $"{field.Label}: {field.Value}", Source: (DescriptionView?)null))
            .ToList();
        entries.AddRange(view.Children.Select(child => (
            $"{child.Label}: {FormatOperatorName(child.View)} [node: {ShortId(child.View.Id)}]",
            (DescriptionView?)child.View)));

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var isLast = index == entries.Count - 1;
            output.Append(prefix).Append(isLast ? "└── " : "├── ").AppendLine(entry.Label);

            if (entry.Source is not null)
            {
                var childPrefix = prefix + (isLast ? "    " : "│   ");
                Append(entry.Source, output, childPrefix, isRoot: true, includeLabel: false);
            }
        }
    }

    private static string FormatOperatorName(DescriptionView view) =>
        $"{view.OperatorName}<{string.Join(", ", view.TypeArguments.Select(TypeFormatter.Format))}>";

    private static string ShortId(string id) =>
        Convert.ToHexString(SHA1.HashData(Encoding.ASCII.GetBytes(id)))[..10].ToLowerInvariant();
}
