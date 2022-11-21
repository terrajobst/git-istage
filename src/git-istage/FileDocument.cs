using System.Text;
using LibGit2Sharp;

namespace GitIStage
{
    internal sealed class FileDocument : Document
    {
        private readonly int _indexOfFirstFile;
        private readonly string[] _lines;
        private readonly TreeEntryChanges[] _changes;

        private FileDocument(int indexOfFirstFile, string[] lines, TreeChanges changes, int width)
        {
            _indexOfFirstFile = indexOfFirstFile;
            _lines = lines;
            _changes = changes.ToArray();
            Width = width;
        }

        public override int Height => _lines.Length;

        public override int Width { get; }

        public override string GetLine(int index)
        {
            return _lines[index];
        }

        public TreeEntryChanges GetChange(int index)
        {
            var changeIndex = index - _indexOfFirstFile;
            if (changeIndex < 0 || changeIndex >= _changes.Length)
                return null;

            return _changes[changeIndex];
        }

        public static FileDocument Create(string repositoryPath, TreeChanges changes, bool viewStage)
        {
            var builder = new StringBuilder();

            builder.AppendLine();

            if (viewStage)
                builder.AppendLine("Changes to be committed:");
            else
                builder.AppendLine("Changes not staged for commit:");

            builder.AppendLine();

            var indent = new string(' ', 8);

            foreach (var c in changes)
            {
                var path = Path.GetRelativePath(repositoryPath, c.Path);
                var change = (c.Status.ToString().ToLower() + ":").PadRight(12);

                builder.Append(indent);
                builder.Append(change);
                builder.Append(path);
                builder.AppendLine();
            }

            var indexOfFirstFile = 3;
            var lines = builder.ToString().Split(Environment.NewLine);

            var width = lines.Select(l => l.Length)
                             .DefaultIfEmpty(0)
                             .Max();

            return new FileDocument(indexOfFirstFile, lines, changes, width);
        }
    }
}