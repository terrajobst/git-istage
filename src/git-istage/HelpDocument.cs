using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitIStage
{
    internal sealed class HelpDocument : Document
    {
        private readonly string[] _lines;

        public HelpDocument(ConsoleCommand[] commands)
        {
            var rows = new List<(string Shortcut, string Description)>(commands.Length);

            foreach (var command in commands)
            {
                var shortcut = command.GetCommandShortcut();
                var description = command.Description;
                rows.Add((shortcut, description));
            }

            var maxShortcutWidth = rows.Select(r => r.Shortcut.Length)
                                       .DefaultIfEmpty(0)
                                       .Max();

            var lines = new List<string>();
            var sb = new StringBuilder();

            foreach (var (shortcut, description) in rows)
            {
                sb.Clear();
                sb.Append(shortcut);
                sb.Append(' ', maxShortcutWidth - shortcut.Length);
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

        public override string GetLine(int index)
        {
            return _lines[index];
        }
    }
}