using System.Text;
using GitIStage.Patches;
using GitIStage.Services;
using GitIStage.Text;
using Patch = GitIStage.Patches.Patch;

namespace GitIStage.UI;

internal sealed class FileDocument : Document
{
    private readonly int _indexOfFirstFile;
    private readonly Patch _patch;
    private readonly bool _viewStage;

    private const int IndexOfFirstFile = 3;
    private const int IndentationWidth = 8;
    private const int ChangeColumnWidth = 14;

    private FileDocument(SourceText sourceText, int indexOfFirstFile, Patch patch, bool viewStage)
        : base(sourceText)
    {
        _indexOfFirstFile = indexOfFirstFile;
        _patch = patch;
        _viewStage = viewStage;
    }

    public Patch Patch => _patch;

    public bool ViewStage => _viewStage;

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

    protected override LineHighlights GetLineHighlights()
    {
        var lineIndex = _indexOfFirstFile;
        var foreground = GetForegroundColor();

        var styles = new List<StyledSpan>();
        
        foreach (var _ in _patch.Entries)
        {
            var line = SourceText.Lines[lineIndex];
            var start = line.Span.Start;

            var changeColumnSpan = new TextSpan(start + IndentationWidth, ChangeColumnWidth);
            var fileNameSpan = TextSpan.FromBounds(changeColumnSpan.End, line.Span.End);
            
            // Change
            styles.Add(new StyledSpan(changeColumnSpan, foreground, null));
    
            // Path
            styles.Add(new StyledSpan(fileNameSpan, Colors.PathText, null));
    
            lineIndex++;
        }
        
        return LineHighlights.Create(SourceText, styles);
    }

    private TextColor GetForegroundColor()
    {
        return _viewStage ? Colors.AddedText : Colors.DeletedText;
    }

    public static FileDocument Create(Patch patch, bool viewStage)
    {
        var builder = new StringBuilder();
        if (patch.Entries.Any())
        {
            builder.AppendLine();
            builder.AppendLine(viewStage ? "Changes to be committed:" : "Changes not staged for commit:");

            var indent = new string(' ', IndentationWidth);

            foreach (var entry in patch.Entries)
            {
                var path = entry.Change == PatchEntryChange.Deleted ? entry.OldPath : entry.NewPath;
                var change = GetChangeText(entry.Change).PadRight(ChangeColumnWidth);

                builder.AppendLine();
                builder.Append(indent);
                builder.Append(change);
                builder.Append(path);
            }
        }

        var sourceText = SourceText.From(builder.ToString());

        return new FileDocument(sourceText, IndexOfFirstFile, patch, viewStage);
    }

    private static string GetChangeText(PatchEntryChange change)
    {
        return change switch
        {
            PatchEntryChange.Added => "added:",
            PatchEntryChange.Deleted => "deleted:",
            PatchEntryChange.Modified => "modified:",
            PatchEntryChange.Renamed => "renamed:",
            PatchEntryChange.Copied => "copied:",
            PatchEntryChange.ModeChanged => "mode changed:",
            _ => throw new ArgumentOutOfRangeException(nameof(change), change, null)
        };
    }
}