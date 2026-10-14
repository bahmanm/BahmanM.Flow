using System.Security.Cryptography;
using System.Text;

namespace BahmanM.Flow.Description.Serialisation;

// Temporary plain-text rendering for the initial Description slice. Replace this and its helpers with ISerialiser for text and JSON output.
internal static class DescriptionFormatter
{
    internal static string Describe<T>(IDescription<T> description)
    {
        var view = description.Accept(new DescriptionViewProjector());
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
        {
            output.Append(FormatOperatorName(view)).Append(" [node: ").Append(ShortId(view.Id)).AppendLine("]");
        }

        var entries = view.Fields
            .Select(field => (Label: $"{field.Label}: {field.Value}", Source: (DescriptionView?)null))
            .ToList();
        if (view.Upstream is not null)
        {
            entries.Add((
                $"upstream: {FormatOperatorName(view.Upstream)} [node: {ShortId(view.Upstream.Id)}]",
                view.Upstream));
        }

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
