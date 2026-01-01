using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using GitIStage.Patches.Headers;
using GitIStage.Text;

namespace GitIStage.Patches;

internal sealed partial class PatchParser
{
    private enum LineKind
    {
        UnknownHeader,
        EndOfFile,

        // Entry
        DiffGitHeader,
        OldPathHeader,
        NewPathHeader,
        OldModeHeader,
        NewModeHeader,
        DeletedFileModeHeader,
        NewFileModeHeader,
        CopyFromHeader,
        CopyToHeader,
        RenameFromHeader,
        RenameToHeader,
        SimilarityIndexHeader,
        DissimilarityIndexHeader,
        IndexHeader,

        // Hunk
        HunkHeader,
        ContextLine,
        AddedLine,
        DeletedLine,
        NoNewLine
    }

    private static LineKind ToLineKind(PatchNodeKind kind)
    {
        switch (kind)
        {
            case PatchNodeKind.DiffGitHeader:
                return LineKind.DiffGitHeader;
            case PatchNodeKind.OldPathHeader:
                return LineKind.OldPathHeader;
            case PatchNodeKind.NewPathHeader:
                return LineKind.NewPathHeader;
            case PatchNodeKind.OldModeHeader:
                return LineKind.OldModeHeader;
            case PatchNodeKind.NewModeHeader:
                return LineKind.NewModeHeader;
            case PatchNodeKind.DeletedFileModeHeader:
                return LineKind.DeletedFileModeHeader;
            case PatchNodeKind.NewFileModeHeader:
                return LineKind.NewFileModeHeader;
            case PatchNodeKind.CopyFromHeader:
                return LineKind.CopyFromHeader;
            case PatchNodeKind.CopyToHeader:
                return LineKind.CopyToHeader;
            case PatchNodeKind.RenameFromHeader:
                return LineKind.RenameFromHeader;
            case PatchNodeKind.RenameToHeader:
                return LineKind.RenameToHeader;
            case PatchNodeKind.SimilarityIndexHeader:
                return LineKind.SimilarityIndexHeader;
            case PatchNodeKind.DissimilarityIndexHeader:
                return LineKind.DissimilarityIndexHeader;
            case PatchNodeKind.IndexHeader:
                return LineKind.IndexHeader;
            case PatchNodeKind.UnknownHeader:
                return LineKind.UnknownHeader;
            case PatchNodeKind.HunkHeader:
                return LineKind.HunkHeader;
            case PatchNodeKind.ContextLine:
                return LineKind.ContextLine;
            case PatchNodeKind.AddedLine:
                return LineKind.AddedLine;
            case PatchNodeKind.DeletedLine:
                return LineKind.DeletedLine;
            case PatchNodeKind.NoNewLine:
                return LineKind.NoNewLine;
            default:
                throw new UnreachableException($"Unexpected line kind {kind}");
        }
    }

    private static readonly ImmutableArray<(Regex Regex, Func<Patch, TextLine, Match, PatchLine> Parse)> LineParsers = [
        (DiffGitRegex(), ParseDiffGitLine),
        (OldPathRegex(), ParseOldPathLine),
        (NewPathRegex(), ParseNewPathLine),
        (OldModeRegex(), ParseOldModeLine),
        (NewModeRegex(), ParseNewModeLine),
        (DeletedFileModeRegex(), ParseDeletedFileModeLine),
        (NewFileModeRegex(), ParseNewFileModeLine),
        (CopyFromRegex(), ParseCopyFromLine),
        (CopyToRegex(), ParseCopyToLine),
        (RenameFromRegex(), ParseRenameFromLine),
        (RenameToRegex(), ParseRenameToLine),
        (SimilarityIndexRegex(), ParseSimilarityIndexLine),
        (DissimilarityIndexRegex(), ParseDissimilarityIndexLine),
        (IndexRegex(), ParseIndexLine),
        (HunkRegex(), ParseHunkHeaderLine),
        (ContextRegex(), ParseContextLine),
        (AddedRegex(), ParseAddedLine),
        (DeletedRegex(), ParseDeletedLine),
        (NoNewLineRegex(), ParseNoNewLine),
    ];

    private static ImmutableArray<PatchLine> ParseLines(Patch patch, SourceText text)
    {
        Debug.Assert(text.Length > 0);

        var fullText = text.ToString();
        var lines = ImmutableArray.CreateBuilder<PatchLine>(text.Lines.Length);

        foreach (var textLine in text.Lines)
        {
            var start = textLine.Start;
            var length = textLine.Length;

            var matchFound = false;
            foreach (var matcher in LineParsers)
            {
                var match = matcher.Regex.Match(fullText, start, length);
                if (match.Success)
                {
                    var line = matcher.Parse(patch, textLine, match);
                    lines.Add(line);
                    matchFound = true;
                    break;
                }
            }

            if (!matchFound)
            {
                var item = new UnknownPatchEntryHeader(patch, textLine);
                lines.Add(item);
            }
        }

        return lines.MoveToImmutable();
    }

    private static DiffGitPatchEntryHeader ParseDiffGitLine(Patch patch, TextLine line, Match match)
    {
        var oldPath = match.Groups["OldPath"].Value;
        var newPath = match.Groups["NewPath"].Value;
        return new DiffGitPatchEntryHeader(patch, line, oldPath, newPath);
    }

    private static OldPathPatchEntryHeader ParseOldPathLine(Patch patch, TextLine line, Match match)
    {
        var path = match.Groups["Path"].Value;
        return new OldPathPatchEntryHeader(patch, line, path);
    }

    private static NewPathPatchEntryHeader ParseNewPathLine(Patch patch, TextLine line, Match match)
    {
        var path = match.Groups["Path"].Value;
        return new NewPathPatchEntryHeader(patch, line, path);
    }

    private static OldModePatchEntryHeader ParseOldModeLine(Patch patch, TextLine line, Match match)
    {
        var mode = ParseMode(line, match, "Mode");
        return new OldModePatchEntryHeader(patch, line, mode);
    }

    private static NewModePatchEntryHeader ParseNewModeLine(Patch patch, TextLine line, Match match)
    {
        var mode = ParseMode(line, match, "Mode");
        return new NewModePatchEntryHeader(patch, line, mode);
    }

    private static DeletedFileModePatchEntryHeader ParseDeletedFileModeLine(Patch patch, TextLine line, Match match)
    {
        var mode = ParseMode(line, match, "Mode");
        return new DeletedFileModePatchEntryHeader(patch, line, mode);
    }

    private static NewFileModePatchEntryHeader ParseNewFileModeLine(Patch patch, TextLine line, Match match)
    {
        var mode = ParseMode(line, match, "Mode");
        return new NewFileModePatchEntryHeader(patch, line, mode);
    }

    private static CopyFromPatchEntryHeader ParseCopyFromLine(Patch patch, TextLine line, Match match)
    {
        var path = match.Groups["Path"].Value;
        return new CopyFromPatchEntryHeader(patch, line, path);
    }

    private static CopyToPatchEntryHeader ParseCopyToLine(Patch patch, TextLine line, Match match)
    {
        var path = match.Groups["Path"].Value;
        return new CopyToPatchEntryHeader(patch, line, path);
    }

    private static RenameFromPatchEntryHeader ParseRenameFromLine(Patch patch, TextLine line, Match match)
    {
        var path = match.Groups["Path"].Value;
        return new RenameFromPatchEntryHeader(patch, line, path);
    }

    private static RenameToPatchEntryHeader ParseRenameToLine(Patch patch, TextLine line, Match match)
    {
        var path = match.Groups["Path"].Value;
        return new RenameToPatchEntryHeader(patch, line, path);
    }

    private static SimilarityIndexPatchEntryHeader ParseSimilarityIndexLine(Patch patch, TextLine line, Match match)
    {
        var percentage = ParsePercentage(line, match, "Number");
        return new SimilarityIndexPatchEntryHeader(patch, line, percentage);
    }

    private static DissimilarityIndexPatchEntryHeader ParseDissimilarityIndexLine(Patch patch, TextLine line, Match match)
    {
        var percentage = ParsePercentage(line, match, "Number");
        return new DissimilarityIndexPatchEntryHeader(patch, line, percentage);
    }

    private static IndexPatchEntryHeader ParseIndexLine(Patch patch, TextLine line, Match match)
    {
        var hash1 = match.Groups["Hash1"].Value;
        var hash2 = match.Groups["Hash1"].Value;
        var mode = ParseOptionalMode(line, match, "Mode");
        return new IndexPatchEntryHeader(patch, line, hash1, hash2, mode);
    }

    private static PatchHunkHeader ParseHunkHeaderLine(Patch patch, TextLine line, Match match)
    {
        var oldLine = ParseInt32(line, match, "OldStart");
        var oldCount = ParseInt32(line, match, "OldLength", 1);
        var newLine = ParseInt32(line, match, "NewStart");
        var newCount = ParseInt32(line, match, "NewLength", 1);
        var function = match.Groups["Function"].Value;

        var header = new PatchHunkHeader(
            patch,
            line,
            oldLine,
            oldCount,
            newLine,
            newCount,
            function
        );

        return header;
    }

    private static PatchHunkLine ParseContextLine(Patch patch, TextLine line, Match match)
    {
        return new PatchHunkLine(patch, PatchNodeKind.ContextLine, line);
    }

    private static PatchHunkLine ParseAddedLine(Patch patch, TextLine line, Match match)
    {
        return new PatchHunkLine(patch, PatchNodeKind.AddedLine, line);
    }

    private static PatchHunkLine ParseDeletedLine(Patch patch, TextLine line, Match match)
    {
        return new PatchHunkLine(patch, PatchNodeKind.DeletedLine, line);
    }

    private static PatchHunkLine ParseNoNewLine(Patch patch, TextLine line, Match match)
    {
        return new PatchHunkLine(patch, PatchNodeKind.NoNewLine, line);
    }

    private static int ParseInt32(TextLine line, Match match, string groupName, int? defaultValue = null)
    {
        var group = match.Groups[groupName];
        var textSpan = group.ValueSpan;

        if (textSpan.Length == 0 && defaultValue is not null)
            return defaultValue.Value;

        if (!int.TryParse(textSpan, out var value))
        {
            var lineNumber = line.Text.GetLineIndex(line.Start) + 1;
            var column = group.Index + 1;
            throw PatchError.ExpectedInt32(lineNumber, column, textSpan);
        }

        return value;
    }

    private static int ParsePercentage(TextLine line, Match match, string groupName)
    {
        var number = ParseInt32(line, match, groupName);

        if (number is <= 0 or > 100)
        {
            var lineNumber = line.Text.GetLineIndex(line.Start) + 1;
            var column = match.Groups[groupName].Index + 1;
            throw PatchError.ExpectedPercentage(lineNumber, column, number);
        }

        return number;
    }

    private static PatchEntryMode ParseOptionalMode(TextLine line, Match match, string groupName)
    {
        var group = match.Groups[groupName];
        return group.Success ? ParseMode(line, match, groupName) : PatchEntryMode.Nonexistent;
    }

    private static PatchEntryMode ParseMode(TextLine line, Match match, string groupName)
    {
        var group = match.Groups[groupName];
        var text = group.Value;

        int value;
        try
        {
            value = Convert.ToInt32(text, 8);
        }
        catch (FormatException)
        {
            var lineNumber = line.Text.GetLineIndex(line.Start) + 1;
            var column = group.Index + 1;
            throw PatchError.ExpectedMode(lineNumber, column, text);
        }

        switch (value)
        {
            case 0x0000:
                return PatchEntryMode.Nonexistent;
            case 0x4000:
                return PatchEntryMode.Directory;
            case 0x81A4:
                return PatchEntryMode.RegularNonExecutableFile;
            case 0x81B4:
                return PatchEntryMode.RegularNonExecutableGroupWriteableFile;
            case 0x81ED:
                return PatchEntryMode.RegularExecutableFile;
            case 0xA000:
                return PatchEntryMode.SymbolicLink;
            case 0xE000:
                return PatchEntryMode.Gitlink;
            default:
            {
                var lineNumber = line.Text.GetLineIndex(line.Start) + 1;
                var column = group.Index + 1;
                throw PatchError.InvalidModeValue(lineNumber, column, value);
            }
        }
    }
}