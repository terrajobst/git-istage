using System.Text;
using GitIStage.Services;
using GitIStage.Text;

namespace GitIStage.UI;

internal sealed class LogDocument : Document
{
    public new static LogDocument Empty { get; } = new ();

    private LogDocument() : this(SourceText.Empty)
    {
    }

    private LogDocument(SourceText sourceText)
        : base(sourceText)
    {
    }

    public override IEnumerable<StyledSpan> GetLineStyles(int index)
    {
        var line = SourceText.Lines[index];
        var lineText = SourceText.AsSpan(line.Span);
        var span = new TextSpan(0, line.Length);

        TextColor foreground;

        if (lineText.StartsWith("! "))
            foreground = TextColor.DarkRed;
        else if (lineText.StartsWith(": ") || lineText.StartsWith("---"))
            foreground = TextColor.DarkGray;
        else
            foreground = TextColor.White;

        return [new StyledSpan(span, foreground, null)];
    }

    public LogDocument Prepend(IReadOnlyCollection<GitOperation> operations)
    {
        var sb = new StringBuilder(SourceText.Length + 1000);

        foreach (var operation in operations.Reverse())
        {
            if (sb.Length > 0)
                AddSeparator(sb);

            sb.Append("git ");
            sb.Append(operation.Command);
            sb.AppendLine();

            if (operation.Result is not null && !operation.Result.Success)
                sb.AppendLine($"! error: exit code = {operation.Result.ExitCode}");

            if (operation.TempFile is not null)
            {
                sb.AppendLine($": Contents of {operation.TempFile.Path}:");

                var contentLines = operation.TempFile.Content()
                    .Trim()
                    .ReplaceLineEndings(Environment.NewLine)
                    .Split(Environment.NewLine);

                foreach (var line in contentLines)
                {
                    sb.Append(": ");
                    sb.AppendLine(line);
                }
            }

            if (operation.Result is not null && !operation.Result.Success)
            {
                foreach (var output in operation.Result.Output)
                    sb.AppendLine(output);
            }
        }

        if (SourceText.Length > 0)
        {
            AddSeparator(sb);
            sb.Append(SourceText.AsSpan());
        }

        return new LogDocument(SourceText.From(sb.ToString()));

        static void AddSeparator(StringBuilder sb)
        {
            sb.AppendLine(new string('-', 30));
        }
    }
}