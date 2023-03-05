using System.Text;
using GitIStage.Commands;

namespace GitIStage.UI;

internal sealed class HelpDocument : Document
{
    private readonly string[] _lines;

    public HelpDocument(IReadOnlyCollection<ConsoleCommand> commands)
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

        var lines = new List<string>();
        var sb = new StringBuilder();

        foreach (var (keyBinding, name, description) in rows)
        {
            sb.Clear();
            sb.Append(keyBinding);
            sb.Append(' ', maxKeyBindingLength - keyBinding.Length);
            sb.Append(" | ");
            sb.Append(name);
            sb.Append(' ', maxNameLength - name.Length);
            sb.Append(" | ");
            sb.Append(description);
            lines.Add(sb.ToString());
        }

        _lines = lines.ToArray();

        Width = _lines.Select(l => l.Length)
                      .DefaultIfEmpty(0)
                      .Max();
    }

    public override int Height => _lines.Length;

    public override int Width { get; }

    public override int EntryCount => _lines.Length;

    public override string GetLine(int index)
    {
        return _lines[index];
    }

    public override int GetLineIndex(int index)
    {
        return index;
    }

    public override int FindEntryIndex(int lineIndex)
    {
        return lineIndex;
    }
}