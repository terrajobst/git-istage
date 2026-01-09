using System.Collections.Immutable;
using GitIStage.Patches.EntryHeaders;
using GitIStage.Patches.HunkLines;
using GitIStage.Text;

namespace GitIStage.Patches;

internal sealed class PatchParser
{
    private readonly PatchTokenizer _tokenizer;

    public PatchParser(Patch root, SourceText text)
    {
        ThrowIfNull(root);
        ThrowIfNull(text);

        _tokenizer = new PatchTokenizer(root, text);
    }

    public ImmutableArray<PatchEntry> ParseEntries()
    {
        var entries = new List<PatchEntry>();

        while (!_tokenizer.IsEndOfFile())
        {
            var entry = ParseEntry();
            entries.Add(entry);
        }

        return entries.ToImmutableArray();
    }

    private PatchEntry ParseEntry()
    {
        var header = ParseEntryHeader();
        var additionalHeaders = ParseEntryAdditionalHeaders();
        var hunks = ParseHunks();
        return new PatchEntry(_tokenizer.Root, header, additionalHeaders, hunks);
    }

    private PatchEntryHeader ParseEntryHeader()
    {
        var diffKeyword = _tokenizer.ParseToken(PatchNodeKind.DiffKeyword);
        _tokenizer.ParseSpace();
        var dashDashToken = _tokenizer.ParseToken(PatchNodeKind.MinusMinusToken);
        var gitKeyword = _tokenizer.ParseToken(PatchNodeKind.GitKeyword);
        _tokenizer.ParseSpace();
        var aPath = _tokenizer.ParsePathUntil("a/", " b/");
        _tokenizer.ParseSpace();
        var bPath = _tokenizer.ParsePath("b/");
        _tokenizer.ParseEndOfLine();
        var result = new PatchEntryHeader(_tokenizer.Root, diffKeyword, dashDashToken, gitKeyword, aPath, bPath);
        _tokenizer.NextLine();
        return result;
    }

    private ImmutableArray<PatchEntryAdditionalHeader> ParseEntryAdditionalHeaders()
    {
        var headers = new List<PatchEntryAdditionalHeader>();

        while (true)
        {
            var header = ParseEntryAdditionalHeader();
            if (header is null)
                break;

            headers.Add(header);
        }

        return headers.ToImmutableArray();
    }

    private PatchEntryAdditionalHeader? ParseEntryAdditionalHeader()
    {
        switch (_tokenizer.GetCurrentChar())
        {
            case '+':
                if (_tokenizer.StartsWith("+++"))
                    return ParseNewPathHeader();
                return null;
            case '-':
                if (_tokenizer.StartsWith("---"))
                    return ParseOldPathHeader();
                return null;
            case 'B':
                if (_tokenizer.StartsWith("Binary"))
                    return ParseBinaryFilesDifferHeader();
                return null;
            case 'c':
                if (_tokenizer.StartsWith("copy from"))
                    return ParseCopyFromHeader();
                if (_tokenizer.StartsWith("copy to"))
                    return ParseCopyToHeader();
                return null;
            case 'd':
                if (_tokenizer.StartsWith("deleted file"))
                    return ParseDeletedFileModeHeader();
                if (_tokenizer.StartsWith("dissimilarity"))
                    return ParseDissimilarityIndexHeader();
                return null;
            case 'i':
                if (_tokenizer.StartsWith("index"))
                    return ParseIndexHeader();
                return null;
            case 'n':
                if (_tokenizer.StartsWith("new mode"))
                    return ParseNewModeHeader();
                if (_tokenizer.StartsWith("new file mode"))
                    return ParseNewFileModeHeader();
                return null;
            case 'o':
                if (_tokenizer.StartsWith("old mode"))
                    return ParseOldModeHeader();
                return null;
            case 'r':
                if (_tokenizer.StartsWith("rename from"))
                    return ParseRenameFromHeader();
                if (_tokenizer.StartsWith("rename to"))
                    return ParseRenameToHeader();
                return null;
            case 's':
                if (_tokenizer.StartsWith("similarity"))
                    return ParseSimilarityIndexHeader();
                return null;
            default:
                return null;
        }
    }

    private OldPathHeader ParseOldPathHeader()
    {
        var dashDashDashToken = _tokenizer.ParseToken(PatchNodeKind.MinusMinusMinusToken);
        _tokenizer.ParseSpace();
        var path = _tokenizer.ParsePath("a/");
        _tokenizer.ParseEndOfLine();
        var result = new OldPathHeader(_tokenizer.Root, dashDashDashToken, path);
        _tokenizer.NextLine();
        return result;
    }

    private NewPathHeader ParseNewPathHeader()
    {
        var plusPlusPlusToken = _tokenizer.ParseToken(PatchNodeKind.PlusPlusPlusToken);
        _tokenizer.ParseSpace();
        var path = _tokenizer.ParsePath("b/");
        _tokenizer.ParseEndOfLine();
        var result = new NewPathHeader(_tokenizer.Root, plusPlusPlusToken, path);
        _tokenizer.NextLine();
        return result;
    }

    private IndexHeader ParseIndexHeader()
    {
        var indexKeyword = _tokenizer.ParseToken(PatchNodeKind.IndexKeyword);
        _tokenizer.ParseSpace();
        var hash1 = _tokenizer.ParseHash();
        var dotDotToken = _tokenizer.ParseToken(PatchNodeKind.DotDotToken);
        var hash2 = _tokenizer.ParseHash();

        PatchTokenHandle<PatchEntryMode>? modeToken = null;

        if (!_tokenizer.IsEndOfLine())
        {
            _tokenizer.ParseSpace();
            modeToken = _tokenizer.ParseMode();
        }

        _tokenizer.ParseEndOfLine();

        var result = new IndexHeader(_tokenizer.Root, indexKeyword, hash1, dotDotToken, hash2, modeToken);
        _tokenizer.NextLine();
        return result;
    }

    private NewFileModeHeader ParseNewFileModeHeader()
    {
        var newKeyword = _tokenizer.ParseToken(PatchNodeKind.NewKeyword);
        _tokenizer.ParseSpace();
        var fileKeyword = _tokenizer.ParseToken(PatchNodeKind.FileKeyword);
        _tokenizer.ParseSpace();
        var modeKeyword = _tokenizer.ParseToken(PatchNodeKind.ModeKeyword);
        _tokenizer.ParseSpace();
        var mode = _tokenizer.ParseMode();
        _tokenizer.ParseEndOfLine();

        var result = new NewFileModeHeader(_tokenizer.Root, newKeyword, fileKeyword, modeKeyword, mode);
        _tokenizer.NextLine();
        return result;
    }

    private DeletedFileModeHeader ParseDeletedFileModeHeader()
    {
        var deletedKeyword = _tokenizer.ParseToken(PatchNodeKind.DeletedKeyword);
        _tokenizer.ParseSpace();
        var fileKeyword = _tokenizer.ParseToken(PatchNodeKind.FileKeyword);
        _tokenizer.ParseSpace();
        var modeKeyword = _tokenizer.ParseToken(PatchNodeKind.ModeKeyword);
        _tokenizer.ParseSpace();
        var mode = _tokenizer.ParseMode();
        _tokenizer.ParseEndOfLine();

        var result = new DeletedFileModeHeader(_tokenizer.Root, deletedKeyword, fileKeyword, modeKeyword, mode);
        _tokenizer.NextLine();
        return result;
    }

    private NewModeHeader ParseNewModeHeader()
    {
        var newKeyword = _tokenizer.ParseToken(PatchNodeKind.NewKeyword);
        _tokenizer.ParseSpace();
        var modeKeyword = _tokenizer.ParseToken(PatchNodeKind.ModeKeyword);
        _tokenizer.ParseSpace();
        var mode = _tokenizer.ParseMode();
        _tokenizer.ParseEndOfLine();


        var result = new NewModeHeader(_tokenizer.Root, newKeyword, modeKeyword, mode);
        _tokenizer.NextLine();
        return result;
    }

    private OldModeHeader ParseOldModeHeader()
    {
        var oldKeyword = _tokenizer.ParseToken(PatchNodeKind.OldKeyword);
        _tokenizer.ParseSpace();
        var modeKeyword = _tokenizer.ParseToken(PatchNodeKind.ModeKeyword);
        _tokenizer.ParseSpace();
        var mode = _tokenizer.ParseMode();
        _tokenizer.ParseEndOfLine();

        var result = new OldModeHeader(_tokenizer.Root, oldKeyword, modeKeyword, mode);
        _tokenizer.NextLine();
        return result;
    }

    private CopyFromHeader ParseCopyFromHeader()
    {
        var copyKeyword = _tokenizer.ParseToken(PatchNodeKind.CopyKeyword);
        _tokenizer.ParseSpace();
        var fromKeyword = _tokenizer.ParseToken(PatchNodeKind.FromKeyword);
        _tokenizer.ParseSpace();
        var path = _tokenizer.ParsePath();
        _tokenizer.ParseEndOfLine();

        var result = new CopyFromHeader(_tokenizer.Root, copyKeyword, fromKeyword, path);
        _tokenizer.NextLine();
        return result;
    }

    private CopyToHeader ParseCopyToHeader()
    {
        var copyKeyword = _tokenizer.ParseToken(PatchNodeKind.CopyKeyword);
        _tokenizer.ParseSpace();
        var toKeyword = _tokenizer.ParseToken(PatchNodeKind.ToKeyword);
        _tokenizer.ParseSpace();
        var path = _tokenizer.ParsePath();
        _tokenizer.ParseEndOfLine();

        var result = new CopyToHeader(_tokenizer.Root, copyKeyword, toKeyword, path);
        _tokenizer.NextLine();
        return result;
    }

    private RenameFromHeader ParseRenameFromHeader()
    {
        var renameKeyword = _tokenizer.ParseToken(PatchNodeKind.RenameKeyword);
        _tokenizer.ParseSpace();
        var fromKeyword = _tokenizer.ParseToken(PatchNodeKind.FromKeyword);
        _tokenizer.ParseSpace();
        var path = _tokenizer.ParsePath();
        _tokenizer.ParseEndOfLine();

        var result = new RenameFromHeader(_tokenizer.Root, renameKeyword, fromKeyword, path);
        _tokenizer.NextLine();
        return result;
    }

    private RenameToHeader ParseRenameToHeader()
    {
        var renameKeyword = _tokenizer.ParseToken(PatchNodeKind.RenameKeyword);
        _tokenizer.ParseSpace();
        var toKeyword = _tokenizer.ParseToken(PatchNodeKind.ToKeyword);
        _tokenizer.ParseSpace();
        var path = _tokenizer.ParsePath();
        _tokenizer.ParseEndOfLine();

        var result = new RenameToHeader(_tokenizer.Root, renameKeyword, toKeyword, path);
        _tokenizer.NextLine();
        return result;
    }

    private SimilarityIndexHeader ParseSimilarityIndexHeader()
    {
        var similarityKeyword = _tokenizer.ParseToken(PatchNodeKind.SimilarityKeyword);
        _tokenizer.ParseSpace();
        var indexKeyword = _tokenizer.ParseToken(PatchNodeKind.IndexKeyword);
        _tokenizer.ParseSpace();
        var percentage = _tokenizer.ParsePercentage();
        _tokenizer.ParseEndOfLine();

        var result = new SimilarityIndexHeader(_tokenizer.Root, similarityKeyword, indexKeyword, percentage);
        _tokenizer.NextLine();
        return result;
    }

    private DissimilarityIndexHeader ParseDissimilarityIndexHeader()
    {
        var dissimilarityKeyword = _tokenizer.ParseToken(PatchNodeKind.DissimilarityKeyword);
        _tokenizer.ParseSpace();
        var indexKeyword = _tokenizer.ParseToken(PatchNodeKind.IndexKeyword);
        _tokenizer.ParseSpace();
        var percentage = _tokenizer.ParsePercentage();
        _tokenizer.ParseEndOfLine();

        var result = new DissimilarityIndexHeader(_tokenizer.Root, dissimilarityKeyword, indexKeyword, percentage);
        _tokenizer.NextLine();
        return result;
    }

    private BinaryFilesDifferHeader ParseBinaryFilesDifferHeader()
    {
        var binaryKeyword = _tokenizer.ParseToken(PatchNodeKind.BinaryKeyword);
        _tokenizer.ParseSpace();
        var filesKeyword = _tokenizer.ParseToken(PatchNodeKind.FilesKeyword);
        _tokenizer.ParseSpace();
        var oldPath = _tokenizer.ParsePathUntil("a/", " and");
        _tokenizer.ParseSpace();
        var andKeyword = _tokenizer.ParseToken(PatchNodeKind.AndKeyword);
        _tokenizer.ParseSpace();
        var newPath = _tokenizer.ParsePathUntil("b/", " differ");
        _tokenizer.ParseSpace();
        var differKeyword = _tokenizer.ParseToken(PatchNodeKind.DifferKeyword);
        _tokenizer.ParseEndOfLine();

        var result = new BinaryFilesDifferHeader(_tokenizer.Root, binaryKeyword, filesKeyword, oldPath, andKeyword, newPath, differKeyword);
        _tokenizer.NextLine();
        return result;
    }

    private ImmutableArray<PatchHunk> ParseHunks()
    {
        var hunks = new List<PatchHunk>();

        while (_tokenizer.StartsWith("@@ "))
        {
            var hunk = ParseHunk();
            hunks.Add(hunk);
        }

        return hunks.ToImmutableArray();
    }

    private PatchHunk ParseHunk()
    {
        var header = ParseHunkHeader();
        var hunkLines = ParseHunkLines();
        return new PatchHunk(_tokenizer.Root, header, hunkLines);
    }

    private PatchHunkHeader ParseHunkHeader()
    {
        var atAt1 = _tokenizer.ParseToken(PatchNodeKind.AtAtToken);
        _tokenizer.ParseSpace();
        var dashToken = _tokenizer.ParseToken(PatchNodeKind.MinusToken);
        var oldRange = _tokenizer.ParseRange();
        _tokenizer.ParseSpace();
        var plusToken = _tokenizer.ParseToken(PatchNodeKind.PlusToken);
        var newRange = _tokenizer.ParseRange();
        _tokenizer.ParseSpace();
        var atAt2 = _tokenizer.ParseToken(PatchNodeKind.AtAtToken);

        PatchTokenHandle<string>? function = null;

        if (!_tokenizer.IsEndOfLine())
        {
            _tokenizer.ParseSpace();
            function = _tokenizer.ParseTextOrEmpty();
        }

        _tokenizer.ParseEndOfLine();

        var result = new PatchHunkHeader(_tokenizer.Root, atAt1, dashToken, oldRange, plusToken, newRange, atAt2, function);
        _tokenizer.NextLine();
        return result;
    }

    private ImmutableArray<PatchHunkLine> ParseHunkLines()
    {
        var hunkLines = new List<PatchHunkLine>();

        while (true)
        {
            var hunkLine = ParseHunkLine();
            if (hunkLine is null)
                break;

            hunkLines.Add(hunkLine);
        }

        return hunkLines.ToImmutableArray();
    }
    
    private PatchHunkLine? ParseHunkLine()
    {
        switch (_tokenizer.GetCurrentChar())
        {
            case ' ':
                return ParseContextLine();
            case '-':
                return ParseDeletedLine();
            case '+':
                return ParseAddedLine();
            case '\\':
                return ParseNoFinalLineBreakLine();
            default:
                // We allow an empty line to stand in for an empty context line.
                // The reason is that trailing whitespaces are often trimmed
                // which would cause the line to not have any marker.
                if (!_tokenizer.IsEndOfFile() && _tokenizer.IsEndOfLine() && _tokenizer.CurrentLine is not null)
                {
                    var marker = _tokenizer.FabricateToken(PatchNodeKind.SpaceToken);
                    var text = _tokenizer.FabricateToken<string>(PatchNodeKind.TextToken, "");
                    _tokenizer.ParseEndOfLine();
                    var result = new ContextLine(_tokenizer.Root, marker, text);
                    _tokenizer.NextLine();
                    return result;
                }
                return null;
        }
    }

    private AddedLine ParseAddedLine()
    {
        var marker = _tokenizer.ParseToken(PatchNodeKind.PlusToken);
        var text = _tokenizer.ParseTextOrEmpty();
        _tokenizer.ParseEndOfLine();

        var result = new AddedLine(_tokenizer.Root, marker, text);
        _tokenizer.NextLine();
        return result;
    }

    private DeletedLine ParseDeletedLine()
    {
        var marker = _tokenizer.ParseToken(PatchNodeKind.MinusToken);
        var text = _tokenizer.ParseTextOrEmpty();
        _tokenizer.ParseEndOfLine();

        var result = new DeletedLine(_tokenizer.Root, marker, text);
        _tokenizer.NextLine();
        return result;
    }

    private ContextLine ParseContextLine()
    {
        var marker = _tokenizer.ParseToken(PatchNodeKind.SpaceToken);
        var text = _tokenizer.ParseTextOrEmpty();
        _tokenizer.ParseEndOfLine();

        var result = new ContextLine(_tokenizer.Root, marker, text);
        _tokenizer.NextLine();
        return result;
    }

    private NoFinalLineBreakLine ParseNoFinalLineBreakLine()
    {
        var marker = _tokenizer.ParseToken(PatchNodeKind.BackslashToken);
        var text = _tokenizer.ParseText();
        _tokenizer.ParseEndOfLine();

        var result = new NoFinalLineBreakLine(_tokenizer.Root, marker, text);
        _tokenizer.NextLine();
        return result;
    }
}