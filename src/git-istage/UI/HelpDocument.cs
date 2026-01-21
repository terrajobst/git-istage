using System.Text;
using GitIStage.Commands;
using GitIStage.Services;
using GitIStage.Text;

namespace GitIStage.UI;

internal sealed class HelpDocument : Document
{
    private readonly int _column1Width;
    private readonly int _column2Width;

    private HelpDocument(SourceText sourceText,
                         int column1Width,
                         int column2Width)
        : base(sourceText)
    {
        ThrowIfLessThanOrEqual(column1Width, 0);
        ThrowIfLessThanOrEqual(column2Width, 0);

        _column1Width = column1Width;
        _column2Width = column2Width;
    }

    protected override LineHighlights GetLineHighlights()
    {
        var styles = new List<StyledSpan>();
        
        foreach (var line in SourceText.Lines)
        {
            var column1Span = new TextSpan(line.Start, _column1Width);
            var separator1Span = new TextSpan(column1Span.End, 1);
            var column2Span = new TextSpan(separator1Span.End, _column2Width);
            var separator2Span = new TextSpan(column2Span.End, 1);
            var column3Span = TextSpan.FromBounds(separator2Span.End, line.End);

            styles.Add(new StyledSpan(column1Span, Colors.CommandKeyForeground, null));
            styles.Add(new StyledSpan(separator1Span, Colors.SeparatorForeground, null));
            styles.Add(new StyledSpan(column2Span, Colors.CommandNameForeground, null));
            styles.Add(new StyledSpan(separator2Span, Colors.SeparatorForeground, null));
            styles.Add(new StyledSpan(column3Span, Colors.CommandDescriptionForeground, null));
        }
        
        return LineHighlights.Create(SourceText, styles);
    }

    public static HelpDocument Create(IReadOnlyCollection<ConsoleCommand> commands)
    {
        var rows = commands.SelectMany(c => c.KeyBindings, (c, k) => (Command: c, KeyBinding: k))
                           .Select(t => (KeyBinding: t.KeyBinding.ToString(), t.Command.Name, t.Command.Description))
                           .ToArray();

        var maxKeyBindingLength = rows.Select(r => r.KeyBinding.Length)
                                      .DefaultIfEmpty(0)
                                      .Max();

        var maxNameLength = rows.Select(r => r.Name.Length)
                                .DefaultIfEmpty(0)
                                .Max();

        var sb = new StringBuilder();

        foreach (var (keyBinding, name, description) in rows)
        {
            sb.Append(keyBinding);
            sb.Append(' ', maxKeyBindingLength - keyBinding.Length);
            sb.Append(" │ ");
            sb.Append(name);
            sb.Append(' ', maxNameLength - name.Length);
            sb.Append(" │ ");
            sb.Append(description);
            sb.AppendLine();
        }

        var column1Width = maxKeyBindingLength + 1;
        var column2Width = maxNameLength + 2;

        var sourceText = SourceText.From(sb.ToString());
        return new HelpDocument(sourceText, column1Width, column2Width);
    }
}