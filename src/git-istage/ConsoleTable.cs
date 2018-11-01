using System;

namespace GitIStage
{
    public class ConsoleTable
    {
        private Row[] rows;
        private int maxShortcutColumnValueLength;
        private int idx = 0;

        public ConsoleTable(int numberOfShortcuts)
        {
            rows = new Row[numberOfShortcuts];
        }

        public void AddRow(string shortcut, string description)
        {
            if (string.IsNullOrWhiteSpace(shortcut))
                throw new ArgumentNullException(nameof(shortcut));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentNullException(nameof(description));

            if (shortcut.Length > maxShortcutColumnValueLength)
                maxShortcutColumnValueLength = shortcut.Length;

            rows[idx] = new Row(shortcut, description);
            idx++;
        }

        public override string ToString()
        {
            string helpPageContent = string.Empty;

            string format = "{0,-" + maxShortcutColumnValueLength + "} | {1}";
       
            foreach (var row in rows)
                helpPageContent += string.Format(format, row.Shortcut, row.Description) + Environment.NewLine;

            return helpPageContent;
        }

        private class Row
        {
            public readonly string Shortcut;
            public readonly string Description;

            public Row(string shortcut, string description)
            {
                Shortcut = shortcut;
                Description = description;
            }
        }
    }
}