using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

using GitIStage.Patching.Headers;
using GitIStage.Text;

namespace GitIStage.Patching;

internal sealed partial class PatchParser
{
    private int _currentLineIndex;

    public PatchParser(Patch root, string text)
    {
        ThrowIfNull(root);
        ThrowIfNullOrEmpty(text);

        Root = root;
        Text = ReplaceDiffCcEntries(text);
        Lines = ParseLines(root, Text);

        static SourceText ReplaceDiffCcEntries(string text)
        {
            const string diffGitHeader = "diff --git ";
            const string diffCcHeader = "diff --cc ";
            
            var sourceText = SourceText.From(text);
            var textSpan = text.AsSpan();
            var sb = (StringBuilder?)null;
            var skipUntilNextEntry = false;
        
            foreach (var line in sourceText.Lines)
            {
                var lineSpan = textSpan.Slice(line.Span);

                if (lineSpan.StartsWith(diffCcHeader, StringComparison.Ordinal))
                {
                    if (sb is null)
                    {
                        sb = new StringBuilder();
                        sb.Append(text, 0, line.Start);
                    }

                    var path = lineSpan.Slice(diffCcHeader.Length);
                    var lineBreak = textSpan.Slice(line.SpanLineBreak);
                    sb.Append($"diff --git a/{path} b/{path}{lineBreak}");
                    sb.Append($"!needs merge{lineBreak}");

                    skipUntilNextEntry = true;
                }
                else
                {
                    if (skipUntilNextEntry && lineSpan.StartsWith(diffGitHeader, StringComparison.Ordinal))
                        skipUntilNextEntry = false;

                    if (!skipUntilNextEntry)
                        sb?.Append(textSpan.Slice(line.SpanIncludingLineBreak));
                }
            }

            if (sb is not null)
            {
                sourceText = SourceText.From(sb.ToString());
            }

            return sourceText;
        }
    }

    public Patch Root { get; }

    public SourceText Text { get; }

    public ImmutableArray<PatchLine> Lines { get; }

    private int CurrentLineNumber => _currentLineIndex + 1;

    private LineKind CurrentLineKind
    {
        get
        {
            if (_currentLineIndex >= Lines.Length)
                return LineKind.EndOfFile;

            return ToLineKind(Lines[_currentLineIndex].Kind);
        }
    }

    private PatchLine? CurrentLine
    {
        get
        {
            if (_currentLineIndex >= Lines.Length)
                return null;

            return Lines[_currentLineIndex];
        }
    }

    private void NextLine()
    {
        _currentLineIndex++;
    }

    private T MatchLine<T>(string errorNameForT)
    {
        if (CurrentLine is T result)
        {
            NextLine();
            return result;
        }

        throw PatchError.ExpectedLine(CurrentLineNumber, errorNameForT);
    }

    public List<PatchEntry> ParseEntries()
    {
        Debug.Assert(Lines.Length > 0);

        var entries = new List<PatchEntry>();

        while (CurrentLineKind != LineKind.EndOfFile)
        {
            var entry = ParseEntry();
            entries.Add(entry);
        }

        Debug.Assert(CurrentLineKind == LineKind.EndOfFile);
        return entries;
    }

    private PatchEntry ParseEntry()
    {
        if (CurrentLineKind != LineKind.DiffGitHeader)
            throw PatchError.ExpectedDiffGitHeader(CurrentLineNumber);

        var headers = new List<PatchEntryHeader>();

        do
        {
            var header = ParseHeader();
            headers.Add(header);
        } while (CurrentLineKind is not LineKind.DiffGitHeader and
                                    not LineKind.HunkHeader and
                                    not LineKind.EndOfFile);
        
        var hunks = new List<PatchHunk>();

        while (CurrentLineKind is LineKind.HunkHeader)
        {
            var hunk = ParseHunk();
            hunks.Add(hunk);
        }

        // NOTE: It's valid to have headers only, e.g. when changing modes or renaming files.

        var diffGitHeader = headers.OfType<DiffGitPatchEntryHeader>().First();

        var oldPathHeader = headers.OfType<OldPathPatchEntryHeader>().FirstOrDefault();
        var newPathHeader = headers.OfType<NewPathPatchEntryHeader>().FirstOrDefault();

        var deletedFileModeHeader = headers.OfType<DeletedFileModePatchEntryHeader>().FirstOrDefault();
        var newFileModeHeader = headers.OfType<NewFileModePatchEntryHeader>().FirstOrDefault();

        var oldModeHeader = headers.OfType<OldModePatchEntryHeader>().FirstOrDefault();
        var newModeHeader = headers.OfType<NewModePatchEntryHeader>().FirstOrDefault();

        var indexHeader = headers.OfType<IndexPatchEntryHeader>().FirstOrDefault();

        var oldPath = newFileModeHeader is not null
                        ? ""
                        : oldPathHeader is not null
                            ? oldPathHeader.Path
                            : diffGitHeader.OldPath;

        var newPath = deletedFileModeHeader is not null
                        ? ""
                        : newPathHeader is not null
                            ? newPathHeader.Path
                            : diffGitHeader.NewPath;

        var oldMode = oldModeHeader is not null
            ? oldModeHeader.Mode
            : deletedFileModeHeader is not null
                ? deletedFileModeHeader.Mode
                : indexHeader?.Mode is not null
                    ? indexHeader.Mode.Value
                    : 0;

        var newMode = newModeHeader is not null
            ? newModeHeader.Mode
            : newFileModeHeader is not null
                ? newFileModeHeader.Mode
                : indexHeader?.Mode is not null
                    ? indexHeader.Mode.Value
                    : 0;

        return new PatchEntry(Root, headers, hunks, oldPath, oldMode, newPath, newMode);
    }

    private PatchEntryHeader ParseHeader()
    {
        return MatchLine<PatchEntryHeader>("entry header");
    }

    private PatchHunk ParseHunk()
    {
        var header = ParseHunkHeader();
        var lines = new List<PatchHunkLine>();

        while (CurrentLineKind is LineKind.ContextLine or
                                  LineKind.AddedLine or
                                  LineKind.DeletedLine or
                                  LineKind.NoNewLine)
        {
            var line = ParseHunkLine();
            lines.Add(line);
        }

        return new PatchHunk(Root, header, lines);
    }

    private PatchHunkHeader ParseHunkHeader()
    {
        return MatchLine<PatchHunkHeader>("hunk header");
    }

    private PatchHunkLine ParseHunkLine()
    {
        return MatchLine<PatchHunkLine>("hunk line");
    }
}