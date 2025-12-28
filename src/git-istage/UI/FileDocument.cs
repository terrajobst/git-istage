using System.Text;
using GitIStage.Patching;

using Patch = GitIStage.Patching.Patch;

namespace GitIStage.UI;

internal sealed class FileDocument : Document
{
    private readonly int _indexOfFirstFile;
    private readonly string[] _lines;
    private readonly Patch _patch;

    private FileDocument(int indexOfFirstFile, string[] lines, Patch patch, int width)
    {
        _indexOfFirstFile = indexOfFirstFile;
        _lines = lines;
        _patch = patch;
        Width = width;
    }

    public override int Height => _lines.Length;

    public override int Width { get; }

    public override int EntryCount => _patch.Entries.Length;

    public override string GetLine(int index)
    {
        return _lines[index];
    }

    public override int GetLineIndex(int index)
    {
        return _indexOfFirstFile + index;
    }

    public override int FindEntryIndex(int lineIndex)
    {
        var changeIndex = lineIndex - _indexOfFirstFile;
        return Math.Min(Math.Max(changeIndex, -1), _patch.Entries.Length - 1);
    }

    public PatchEntry? GetEntry(int index)
    {
        var changeIndex = index - _indexOfFirstFile;
        if (changeIndex < 0 || changeIndex >= _patch.Entries.Length)
            return null;

        return _patch.Entries[changeIndex];
    }

    public static FileDocument Create(string? patchText, bool viewStage)
    {
        var patch = Patch.Parse(patchText ?? string.Empty);

        var builder = new StringBuilder();
        if (patch.Entries.Any())
        {
            builder.AppendLine();
            builder.AppendLine(viewStage ? "Changes to be committed:" : "Changes not staged for commit:");

            var indent = new string(' ', 8);

            foreach (var entry in patch.Entries)
            {
                var path = entry.ChangeKind == PatchEntryChangeKind.Deleted ? entry.OldPath : entry.NewPath;
                var change = (entry.ChangeKind.ToString().ToLower() + ":").PadRight(12);

                builder.AppendLine();
                builder.Append(indent);
                builder.Append(change);
                builder.Append(path);
            }
        }

        const int indexOfFirstFile = 3;
        var lines = builder.Length == 0
                    ? Array.Empty<string>()
                    : builder.ToString().Split(Environment.NewLine);

        var width = lines.Select(l => l.Length)
                         .DefaultIfEmpty(0)
                         .Max();

        return new FileDocument(indexOfFirstFile, lines, patch, width);
    }
}