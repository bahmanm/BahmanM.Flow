using System.Text;

namespace BahmanM.Flow.Description.Serialisation;

// Temporary plain-text rendering for the initial Description slice. Replace this and its helpers with ISerialiser for text and JSON output.
internal static class DescriptionFormatter
{
    internal static string Describe<T>(IDescription<T> description)
    {
        var output = new StringBuilder();
        Append(description, output, string.Empty, isLast: true, isRoot: true, includeLabel: true);
        return output.ToString().TrimEnd();
    }

    private static void Append(
        IDescription description,
        StringBuilder output,
        string prefix,
        bool isLast,
        bool isRoot,
        bool includeLabel)
    {
        if (!isRoot && includeLabel)
            output.Append(prefix).Append(isLast ? "└── " : "├── ");

        if (includeLabel)
            output.Append(description.OperatorDisplayName).Append(" [node: ").Append(description.ShortId).AppendLine("]");

        var entries = new List<(string Label, bool IsSource)>();
        if (description is IOperation operation)
            entries.Add(($"operation: {OperationFormatter.Format(operation.Operation)}", false));
        if (description is IValue value)
            entries.Add(($"value: {DescriptionValueFormatter.Format(value.Value, description.ValueType)}", false));
        if (description is IFailure failure)
            entries.Add(($"exception: {TypeFormatter.Format(failure.Exception.GetType())}", false));
        if (description.Source is not null)
            entries.Add(($"upstream: {description.Source.OperatorDisplayName} [node: {description.Source.ShortId}]", true));

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var last = index == entries.Count - 1;
            output.Append(prefix).Append(last ? "└── " : "├── ").AppendLine(entry.Label);

            if (entry.IsSource)
            {
                var childPrefix = prefix + (last ? "    " : "│   ");
                Append(description.Source!, output, childPrefix, isLast: true, isRoot: true, includeLabel: false);
            }
        }
    }
}
