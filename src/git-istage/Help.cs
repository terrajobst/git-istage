using System;
using System.Collections.Generic;

namespace GitIStage
{
    internal class Help
    {
        public PatchDocument GetKeyboardShortcutsInfo(ConsoleCommand[] commands)
        {
            var table = new ConsoleTable(commands.Length);

            foreach (var command in commands)
            {
                table.AddRow(command.GetCommandShortcut(), command.Description);
            }

            string[] lines = table.ToString().Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var patchLines = new List<PatchLine>(lines.Length);
            foreach (var line in lines)
            {
                patchLines.Add(new PatchLine(PatchLineKind.Context, line));
            }

            return new PatchDocument(null, patchLines);
        }
    }
}
