using System.Text;
using GitIStage.Commands;
using GitIStage.Text;

namespace GitIStage.UI;

internal sealed class HelpDocument : Document
{

    private HelpDocument(SourceText sourceText)
        : base(sourceText)
    {
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
            sb.Append(" | ");
            sb.Append(name);
            sb.Append(' ', maxNameLength - name.Length);
            sb.Append(" | ");
            sb.Append(description);
            sb.AppendLine();
        }

        var sourceText = SourceText.From(sb.ToString());
        return new HelpDocument(sourceText);
    }
}