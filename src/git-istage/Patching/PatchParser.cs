using System.Diagnostics;
using GitIStage.Patching.Headers;
using GitIStage.Patching.Text;

namespace GitIStage.Patching;

internal sealed partial class PatchParser
{
    public PatchParser(string text)
    {
        ThrowIfNullOrEmpty(text);

        _text = SourceText.From(text);
        _lines = ParseLines(_text);
    }

    private readonly SourceText _text;
    private readonly List<PatchLine> _lines;
    private int _currentLineIndex;

    private int CurrentLineNumber => _currentLineIndex + 1;

    private LineKind CurrentLineKind
    {
        get
        {
            if (_currentLineIndex >= _lines.Count)
                return LineKind.EndOfFile;

            return ToLineKind(_lines[_currentLineIndex].Kind);
        }
    }

    private PatchLine? CurrentLine
    {
        get
        {
            if (_currentLineIndex >= _lines.Count)
                return null;

            return _lines[_currentLineIndex];
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

    public Patch ParsePatch()
    {
        Debug.Assert(_lines.Count > 0);

        var entries = new List<PatchEntry>();

        while (CurrentLineKind != LineKind.EndOfFile)
        {
            var entry = ParseEntry();
            entries.Add(entry);
        }

        Debug.Assert(CurrentLineKind == LineKind.EndOfFile);

        return new Patch(_text, entries);
    }

    private PatchEntry ParseEntry()
    {
        if (CurrentLineKind != LineKind.DiffGitHeader)
            throw PatchError.ExpectedDiffGitHeader(CurrentLineNumber);

        var headers = new List<PatchEntryHeader>();
        var hunks = new List<PatchHunk>();

        while (CurrentLineKind != LineKind.EndOfFile)
        {
            if (CurrentLineKind == LineKind.HunkHeader)
            {
                // The entry condition in this method is that we start off with a 'diff --git' header,
                // which we already parsed below.
                Debug.Assert(headers.Count > 0);

                var hunk = ParseHunk();
                hunks.Add(hunk);
            }
            else
            {
                // If it's not a hunk header, we treat the line as an entry header.
                // And if it's not a known header, we'll parse it as an unknown header.
                //
                // However, either only valid if we haven't seen any hunks yet.

                if (hunks.Count > 0)
                    throw PatchError.ExpectedHunkHeader(CurrentLineNumber);

                var header = ParseHeader();
                headers.Add(header);
            }
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

        return new PatchEntry(headers, hunks, oldPath, oldMode, newPath, newMode);
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

        return new PatchHunk(header, lines);
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