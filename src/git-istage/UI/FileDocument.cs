using System.Text;
using GitIStage.Patches;
using GitIStage.Text;
using Patch = GitIStage.Patches.Patch;

namespace GitIStage.UI;

internal sealed class FileDocument : Document
{
    private readonly int _indexOfFirstFile;
    private readonly Patch _patch;

    private FileDocument(SourceText sourceText, int indexOfFirstFile, Patch patch)
        : base(sourceText)
    {
        _indexOfFirstFile = indexOfFirstFile;
        _patch = patch;
    }

    public Patch Patch => _patch;

    public PatchEntry? GetEntry(int index)
    {
        var changeIndex = index - _indexOfFirstFile;
        if (changeIndex < 0 || changeIndex >= _patch.Entries.Length)
            return null;

        return _patch.Entries[changeIndex];
    }

    public int GetLineIndex(PatchEntry entry)
    {
        var entryIndex = _patch.Entries.IndexOf(entry);
        return _indexOfFirstFile + entryIndex;
    }

    public override IEnumerable<StyledSpan> GetLineStyles(int index)
    {
        var entry = GetEntry(index);
        if (entry is not null)
        {
            var line = GetLine(index);
            var lineLength = line.Length;
            var foreground = GetForegroundColor(entry);

            // Change
            yield return new StyledSpan(new TextSpan(8, 12), foreground, null);

            // Path
            yield return new StyledSpan(TextSpan.FromBounds(20, lineLength), ConsoleColor.DarkCyan, null);
        }
    }

    private static ConsoleColor? GetForegroundColor(PatchEntry changes)
    {
        switch (changes.ChangeKind)
        {
            case PatchEntryChangeKind.Added:
            case PatchEntryChangeKind.Copied:
                return ConsoleColor.DarkGreen;
            case PatchEntryChangeKind.Renamed:
            case PatchEntryChangeKind.Deleted:
            case PatchEntryChangeKind.Modified:
                return ConsoleColor.DarkRed;
            default:
                return null;
        }
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
        var sourceText = SourceText.From(builder.ToString());

        return new FileDocument(sourceText, indexOfFirstFile, patch);
    }
}