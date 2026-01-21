using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using GitIStage.Patches;
using GitIStage.Text;
using GitIStage.UI;
using GitIStagePatch = GitIStage.Patches.Patch;

using IStateStack = TextMateSharp.Grammars.IStateStack;
using StateStack = TextMateSharp.Grammars.StateStack;
using IGrammar = TextMateSharp.Grammars.IGrammar;
using System.Diagnostics;
using TextMateSharp.Themes;
using System.Globalization;
using System.Runtime.InteropServices;
using LibGit2Sharp;

namespace GitIStage.Services;

internal sealed class DocumentService
{
    private readonly GitService _gitService;

    private GitIStagePatch _workingCopyPatch;
    private GitIStagePatch _stagePatch;
    private bool _fullFileDiff;
    private int _contextLines = 3;
    private bool _syntaxHighlighting = true;

    private DocumentState _documentState = DocumentState.Empty; 
    private FileDocument _workingCopyFilesDocument;
    private FileDocument _stageFilesDocument;

    public DocumentService(GitService gitService, FileWatchingService? fileWatchingService)
    {
        _gitService = gitService;
        _gitService.RepositoryChanged += GitServiceOnRepositoryChanged;
        fileWatchingService?.Changed += FileWatchingServiceOnChanged;
        RecomputePatch();
    }

    public GitIStagePatch WorkingCopyPatch => _workingCopyPatch;

    public GitIStagePatch StagePatch => _stagePatch;

    public PatchDocument WorkingCopyPatchDocument => _documentState.WorkingCopyDocument;

    public FileDocument WorkingCopyFilesDocument => _workingCopyFilesDocument;

    public PatchDocument StagePatchDocument => _documentState.StageDocument;

    public FileDocument StageFilesDocument => _stageFilesDocument;

    public bool ViewFullDiff
    {
        get => _fullFileDiff;
        set
        {
            if (_fullFileDiff != value)
            {
                _fullFileDiff = value;
                RecomputePatch();
            }
        }
    }

    public int ContextLines
    {
        get => _contextLines;
        set
        {
            if (_contextLines != value)
            {
                _contextLines = value;
                RecomputePatch();
            }
        }
    }

    public bool SyntaxHighlighting
    {
        get => _syntaxHighlighting;
        set
        {
            if (_syntaxHighlighting != value)
            {
                _syntaxHighlighting = value;
                if (!_syntaxHighlighting)
                    SetDocumentState(_documentState.DropHighlights());
                else
                    UpdateDocuments();
            }
        }
    }

    [MemberNotNull(nameof(_workingCopyPatch))]
    [MemberNotNull(nameof(_stagePatch))]
    [MemberNotNull(nameof(_workingCopyFilesDocument))]
    [MemberNotNull(nameof(_stageFilesDocument))]
    public void RecomputePatch()
    {
        _workingCopyPatch = GetWorkingCopyPatch();
        _stagePatch = GetStagePatch();
        UpdateDocuments();
    }

    private void UpdatePatch(ImmutableArray<string> updatedPaths,
                             bool skipIndex = false)
    {
        var patchForUpdatedPaths = GetWorkingCopyPatch(updatedPaths);
        var result = _workingCopyPatch.Update(patchForUpdatedPaths, updatedPaths);

        _workingCopyPatch = result;
        if (!skipIndex)
            _stagePatch = GetStagePatch();
        UpdateDocuments();
    }

    private GitIStagePatch GetWorkingCopyPatch(IEnumerable<string>? affectedPaths = null)
    {
        return GitIStagePatch.Parse(_gitService.GetPatch(_fullFileDiff, _contextLines, stage: false, affectedPaths));
    }

    private GitIStagePatch GetStagePatch()
    {
        return GitIStagePatch.Parse(_gitService.GetPatch(_fullFileDiff, _contextLines, stage: true));
    }

    [MemberNotNull(nameof(_workingCopyFilesDocument))]
    [MemberNotNull(nameof(_stageFilesDocument))]
    public async void UpdateDocuments()
    {
        var workingCopyPatch = _workingCopyPatch;
        var stagePatch = _stagePatch;

        if (_documentState.ShouldPerformIncrementalUpdate(workingCopyPatch, stagePatch, _syntaxHighlighting))
        {
            var state = _documentState.IncrementalUpdate(workingCopyPatch, stagePatch, SyntaxTheme.Instance);
            SetDocumentState(state);
        }
        else
        {
            // This is non-incremental update. To speed things up, let's first create
            // the patch without any syntax highlighting.
            var stateWithoutHighlights = DocumentState.Create(_workingCopyPatch, _stagePatch);
            SetDocumentState(stateWithoutHighlights);

            // If we don't want syntax highlighting, we're done.
            if (!_syntaxHighlighting)
                return;
            
            // OK let's perform syntax highlighting in the background.
            var stateWithHighlights = await Task.Run(() => DocumentState.CreateHighlighted(workingCopyPatch, stagePatch, SyntaxTheme.Instance));

            // If the state is unchanged, replace it with the highlighted state.
            if (_documentState == stateWithoutHighlights)
                SetDocumentState(stateWithHighlights);
        }
    }

    [MemberNotNull(nameof(_workingCopyFilesDocument))]
    [MemberNotNull(nameof(_stageFilesDocument))]
    private void SetDocumentState(DocumentState state)
    {
        _documentState = state;
        _workingCopyFilesDocument = FileDocument.Create(_workingCopyPatch, viewStage: false);
        _stageFilesDocument = FileDocument.Create(_stagePatch, viewStage: true);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void GitServiceOnRepositoryChanged(object? sender, RepositoryChangedEventArgs e)
    {
        if (e.AffectedPaths.Any())
            UpdatePatch(e.AffectedPaths);
        else
            RecomputePatch();
    }

    private void FileWatchingServiceOnChanged(object? sender, FileWatchingEventArgs args)
    {
        var workingDirectory = _gitService.Repository.Info.WorkingDirectory;
        var repositoryDirectory = _gitService.Repository.Info.Path;

        var pathsInWorkingCopyPatch = _workingCopyPatch
            .Entries
            .Select(e => Path.GetFullPath(Path.Join(workingDirectory, e.NewPath)))
            .ToHashSet(StringComparer.Ordinal);

        var entriesToAddOrUpdate = new SortedSet<string>(StringComparer.Ordinal);
        var indexWasChanged = false;

        foreach (var @event in args.Events)
        {
            if (@event is not RenamedEventArgs rename)
            {
                if (!_gitService.IsIgnoredOrOutsideWorkingDirectory(@event.FullPath))
                {
                    if (File.Exists(@event.FullPath))
                        entriesToAddOrUpdate.Add(@event.FullPath);
                }
            }
            else
            {
                var isChangeInGitDirectory = rename.OldFullPath.StartsWith(repositoryDirectory, StringComparison.Ordinal);
                if (isChangeInGitDirectory)
                {
                    var isIndexChange = string.Equals(rename.OldFullPath, Path.Join(repositoryDirectory, "index.lock"), StringComparison.Ordinal) &&
                                        string.Equals(rename.FullPath, Path.Join(repositoryDirectory, "index"), StringComparison.Ordinal);

                    if (isIndexChange)
                        indexWasChanged = true;
                }
                else
                {
                    AddRename(rename.OldFullPath);
                    AddRename(rename.FullPath);

                    foreach (var affectedOldPath in pathsInWorkingCopyPatch.Where(p => p.StartsWith(rename.OldFullPath, StringComparison.Ordinal)))
                    {
                        var suffix = affectedOldPath.Substring(rename.OldFullPath.Length);
                        var affectedNewPath = rename.FullPath + suffix;

                        AddRename(affectedOldPath);
                        AddRename(affectedNewPath);
                    }

                    void AddRename(string path)
                    {
                        if (!_gitService.IsIgnoredOrOutsideWorkingDirectory(path))
                            entriesToAddOrUpdate.Add(path);
                    }
                }
            }
        }

        if (indexWasChanged)
        {
            _gitService.InitializeRepository();
            var stagePatchOld = _stagePatch;
            _stagePatch = GetStagePatch();

            var beforePaths = stagePatchOld.Entries.Select(e => Path.GetFullPath(Path.Join(workingDirectory, e.NewPath))).ToHashSet(StringComparer.Ordinal);
            var afterPaths = _stagePatch.Entries.Select(e => Path.GetFullPath(Path.Join(workingDirectory, e.NewPath))).ToHashSet(StringComparer.Ordinal);

            var newPaths = afterPaths.Except(beforePaths, StringComparer.Ordinal);
            var removedPaths = beforePaths.Except(afterPaths, StringComparer.Ordinal);

            entriesToAddOrUpdate.UnionWith(newPaths);
            entriesToAddOrUpdate.UnionWith(removedPaths);
        }

        ToRepoPaths(ref entriesToAddOrUpdate, workingDirectory);

        static void ToRepoPaths(ref SortedSet<string> set, string workingDirectory)
        {
            set = new SortedSet<string>(set.Select(p => Path.GetRelativePath(workingDirectory, p).Replace(Path.DirectorySeparatorChar, '/')));
        }

        if (entriesToAddOrUpdate.Count > 0)
        {
            UpdatePatch([..entriesToAddOrUpdate], skipIndex: indexWasChanged);
        }
    }

    public event EventHandler? Changed;
}

public sealed class TextLines : IEnumerable<ReadOnlyMemory<char>>
{
    public static TextLines Empty { get; } = new(ImmutableArray<ReadOnlyMemory<char>>.Empty);
    
    private readonly ImmutableArray<ReadOnlyMemory<char>> _lines;

    private TextLines(ImmutableArray<ReadOnlyMemory<char>> lines)
    {
        _lines = lines;
    }
    
    public static TextLines FromFile(string fileName)
    {
        if (!File.Exists(fileName))
            return Empty;
        
        var lines = File.ReadLines(fileName)
                        .Select(l => l.AsMemory())
                        .ToImmutableArray();
        return new TextLines(lines);
    }

    public static TextLines FromStream(Stream stream)
    {
        var lines = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();
        using var reader = new StreamReader(stream);

        while (reader.ReadLine() is { } line)
            lines.Add(line.AsMemory());

        return new TextLines(lines.ToImmutable());
    }

    public ReadOnlyMemory<char> this[int index] => _lines[index];

    public int Count => _lines.Length;

    public TextLines ApplyReversed(PatchEntry? patchEntry)
    {
        if (patchEntry is null)
            return this;

        var result = new List<ReadOnlyMemory<char>>();
        var text = patchEntry.Root.Text;
        var lastLine = 0; 

        foreach (var hunk in patchEntry.Hunks)
        {
            var startLine = hunk.NewRange.LineNumber - 1;
            CopyLinesTo(result, lastLine, startLine);

            foreach (var (hunkLineIndex, _, _) in hunk.EnumerateIndices())
            {
                var line = hunk.Lines[hunkLineIndex];
                if (line.Kind is PatchNodeKind.ContextLine or
                                 PatchNodeKind.DeletedLine)
                {
                    var lineSpan = TextSpan.FromBounds(line.Span.Start + 1, line.Span.End);
                    var lineText = text.AsMemory(lineSpan);
                    result.Add(lineText);
                }
            }

            lastLine = hunk.NewRange.LineNumber - 1 + hunk.NewRange.Length;
        }

        CopyLinesTo(result, lastLine, _lines.Length);
        
        return new TextLines([..result]);
    }
    
    private void CopyLinesTo(List<ReadOnlyMemory<char>> target, int startLine, int endLine)
    {
        if (startLine < 0 || endLine < 0)
            return;

        var span = _lines.AsSpan(startLine, endLine - startLine);
        target.AddRange(span);
    }

    public IEnumerator<ReadOnlyMemory<char>> GetEnumerator()
    {
        return ((IEnumerable<ReadOnlyMemory<char>>) _lines).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
#if DEBUG
    public bool SequenceEqual(TextLines other)
    {
        return _lines.SequenceEqual(other._lines, (x, y) => x.Span.SequenceEqual(y.Span));
    }
#endif
}

public sealed class TextLinesWithStates
{
    public static TextLinesWithStates Create(TextLines lines, IGrammar grammar)
    {
        var states = new List<IStateStack?>(lines.Count);
        var state = (IStateStack?)null;

        foreach (var line in lines)
        {
            states.Add(state);
            var lineResult = grammar.TokenizeLine2(line, state, Timeout.InfiniteTimeSpan);
            state = lineResult.RuleStack;
        }

        return new TextLinesWithStates(lines, [..states]);
    }

    private TextLinesWithStates(TextLines lines, ImmutableArray<IStateStack?> states)
    {
        Lines = lines;
        States = states;
    }

    public TextLines Lines { get; }

    public ImmutableArray<IStateStack?> States { get; }
}

public sealed class LineHighlights
{
    public static LineHighlights Empty { get; } = new(ImmutableArray<ImmutableArray<StyledSpan>>.Empty);
    
    private readonly ImmutableArray<ImmutableArray<StyledSpan>> _lineStyles;

    private LineHighlights(ImmutableArray<ImmutableArray<StyledSpan>> lineStyles)
    {
        _lineStyles = lineStyles;
    }

    public static LineHighlights Create(SourceText text, IReadOnlyCollection<StyledSpan> spans, int offset = 0)
    {
        var start = offset;
        var end = spans.Select(s => s.Span.End).DefaultIfEmpty(offset).Last();
        var startLine = text.GetLineIndex(start);
        var endLine = text.GetLineIndex(end);
        var lineCount = endLine - startLine + 1;

        var lineBuilder = ImmutableArray.CreateBuilder<ImmutableArray<StyledSpan>>(lineCount);
        for (var i = 0; i < lineCount; i++)
            lineBuilder.Add(ImmutableArray<StyledSpan>.Empty);

        var styleBuilder = ImmutableArray.CreateBuilder<StyledSpan>();
        var previousIndex = -1;
        
        foreach (var styledSpan in spans)
        {
            var style = styledSpan.Style;
            var span = styledSpan.Span;
            var originalLineIndex = text.GetLineIndex(span.Start);
            var line = text.Lines[originalLineIndex];
            var index = originalLineIndex - startLine;

            if (previousIndex >= 0 && index != previousIndex)
            {
                CommitLine(previousIndex, lineBuilder, styleBuilder);
            }

            var adjustedSpan = new TextSpan(span.Start - line.Start, span.Length);
            var adjustedStyledSpan = new StyledSpan(adjustedSpan, style);
            styleBuilder.Add(adjustedStyledSpan);

            previousIndex = index;
        }

        CommitLine(previousIndex, lineBuilder, styleBuilder);
        return new LineHighlights(lineBuilder.ToImmutable());

        static void CommitLine(
            int index,
            ImmutableArray<ImmutableArray<StyledSpan>>.Builder lineBuilder,
            ImmutableArray<StyledSpan>.Builder styleBuilder)
        {
            if (styleBuilder.Count == 0)
                return;

            lineBuilder[index] = styleBuilder.ToImmutable();
            styleBuilder.Clear();
        }
    }
    
    public ImmutableArray<StyledSpan> this[int index] => _lineStyles[index];
    
    public int Count => _lineStyles.Length;
}

public sealed class PatchEntryWithHighlights
{
    private PatchEntryWithHighlights(PatchEntry entry, LineHighlights lineHighlights)
    {
        ThrowIfNull(entry);
        ThrowIfNull(lineHighlights);

        Entry = entry;
        LineHighlights = lineHighlights;
    }

    public PatchEntry Entry { get; }

    public LineHighlights LineHighlights { get; }

    public static PatchEntryWithHighlights Create(TextLinesWithStates original, PatchEntry patch, IGrammar grammar)
    {
        if (patch.Hunks.Length == 0)
            return new PatchEntryWithHighlights(patch, LineHighlights.Empty);
        
        var receiver = new List<StyledSpan>();
        var text = patch.Root.Text;
        var reHighlightOldLineStart = -1;
        var newState = (IStateStack?)null;

        foreach (var hunk in patch.Hunks)
        {
            // old states will always have at least one line. And the first
            // state is always null, hence we can fold -1 and 0 both to 0.
            var oldLine = int.Max(0, hunk.OldRange.LineNumber - 1);

            if (reHighlightOldLineStart < 0)
            {
                newState = oldLine < original.States.Length
                    ? original.States[oldLine]
                    : null;
            }
            else
            {
                Debug.Assert(newState is not null);

                for (var line = reHighlightOldLineStart; line < oldLine; line++)
                {
                    var oldLineMemory = original.Lines[line];
                    GetHighlightState(grammar, oldLineMemory, ref newState);
                }
            }

            foreach (var hunkLine in hunk.Lines)
            {
                var lineStart = hunkLine.Span.Start + 1;
                var lineSpan = TextSpan.FromBounds(lineStart, hunkLine.Span.End);
                var lineText = text.AsMemory(lineSpan);

                if (hunkLine.Kind == PatchNodeKind.ContextLine)
                {
                    GetHighlights(grammar, lineStart, lineText, ref newState, receiver);
                    oldLine++;
                }
                else if (hunkLine.Kind == PatchNodeKind.AddedLine)
                {
                    GetHighlights(grammar, lineStart, lineText, ref newState, receiver);
                }
                else if (hunkLine.Kind == PatchNodeKind.DeletedLine)
                {
                    GetHighlights(grammar, lineStart, lineText, original.States[oldLine], receiver);
                }
            }

            if (oldLine < original.States.Length)
                reHighlightOldLineStart = SameState(newState, original.States[oldLine]) ? -1 : oldLine;
        }

        var offset = patch.Hunks.First().Lines.First().Span.Start;
        var lineHighlights = LineHighlights.Create(patch.Root.Text, receiver, offset);
        return new PatchEntryWithHighlights(patch, lineHighlights);
    }

    private static bool SameState(IStateStack? a, IStateStack? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        var stateA = (StateStack)a;
        var stateB = (StateStack)b;

        // TODO: public AttributedScopeStack NameScopesList { get; private set; }
        // TODO: public AttributedScopeStack ContentNameScopesList { get; private set; }

        return stateA.RuleId == stateB.RuleId &&
                stateA.Depth == stateB.Depth &&
                stateA.EndRule == stateB.EndRule &&
                stateA.BeginRuleCapturedEOL == stateB.BeginRuleCapturedEOL &&
                SameState(stateA.Parent, stateB.Parent);
    }

    private static void GetHighlights(IGrammar grammar, int lineStart, ReadOnlyMemory<char> text, IStateStack? state, ICollection<StyledSpan> receiver)
    {
        GetHighlights(grammar, lineStart, text, ref state, receiver);
    }

    private static void GetHighlights(IGrammar grammar, int lineStart, ReadOnlyMemory<char> text, ref IStateStack? state, ICollection<StyledSpan> receiver)
    {
        var tokenizedLine = grammar.TokenizeLine(text, state, TimeSpan.MaxValue);
        state = tokenizedLine.RuleStack;

        foreach (var token in tokenizedLine.Tokens)
        {
            var startIndex = token.StartIndex > text.Length ? text.Length : token.StartIndex;
            var endIndex = token.EndIndex > text.Length ? text.Length : token.EndIndex;

            var foreground = -1;
            var background = -1;
            var fontStyle = FontStyle.None;

            foreach (var themeRule in SyntaxTheme.Instance.Theme.Match(token.Scopes))
            {
                if (foreground == -1 && themeRule.foreground > 0)
                    foreground = themeRule.foreground;

                if (background == -1 && themeRule.background > 0)
                    background = themeRule.background;

                if (fontStyle == FontStyle.None && themeRule.fontStyle != FontStyle.None)
                    fontStyle = themeRule.fontStyle;
            }

            var style = new TextStyle
            {
                Foreground = GetColor(foreground),
                Background = GetColor(background),
                Attributes = GetAttributes(fontStyle)
            };

            var span = new TextSpan(lineStart + startIndex, endIndex - startIndex);
            var styledSpan = new StyledSpan(span, style);
            receiver.Add(styledSpan);
        }
    }

    private static void GetHighlightState(IGrammar grammar, ReadOnlyMemory<char> text, ref IStateStack? state)
    {
        // Note: we use TokenizeLine2 to avoid the overhead of creating tokens
        var tokenizedLine = grammar.TokenizeLine2(text, state, TimeSpan.MaxValue);
        state = tokenizedLine.RuleStack;
    }

    private static TextColor? GetColor(int colorId)
    {
        if (colorId == -1)
            return null;

        return HexToColor(SyntaxTheme.Instance.Theme.GetColor(colorId));
    }

    private static TextColor HexToColor(string hexString)
    {
        if (hexString.Length != 7 || hexString[0] != '#')
            throw new FormatException();

        var r = byte.Parse(hexString.AsSpan(1, 2), NumberStyles.AllowHexSpecifier);
        var g = byte.Parse(hexString.AsSpan(3, 2), NumberStyles.AllowHexSpecifier);
        var b = byte.Parse(hexString.AsSpan(5, 2), NumberStyles.AllowHexSpecifier);

        return new TextColor(r, g, b);
    }

    private static TextAttributes GetAttributes(FontStyle fontStyle)
    {
        var result = TextAttributes.None;

        if (fontStyle == FontStyle.NotSet)
            return result;

        if ((fontStyle & FontStyle.Italic) != 0)
            result |= TextAttributes.Italic;

        if ((fontStyle & FontStyle.Bold) != 0)
            result |= TextAttributes.Bold;

        if ((fontStyle & FontStyle.Underline) != 0)
            result |= TextAttributes.Underline;

        if ((fontStyle & FontStyle.Strikethrough) != 0)
            result |= TextAttributes.Strike;

        return result;
    }
}

internal sealed class DocumentState
{
    private static readonly FrozenDictionary<PatchEntry, LineHighlights> NoHighlights = FrozenDictionary<PatchEntry, LineHighlights>.Empty;

    private static readonly PatchDocument EmptyPatchDocument = new(GitIStagePatch.Empty, NoHighlights);

    public static DocumentState Empty { get; } = new(EmptyPatchDocument, EmptyPatchDocument);
    
    private DocumentState(PatchDocument workingCopyDocument, PatchDocument stageDocument)
    {
        WorkingCopyDocument = workingCopyDocument;
        StageDocument = stageDocument;
    }

    public bool HasHighlighting => WorkingCopyDocument.Highlights != NoHighlights ||
                                   StageDocument.Highlights != NoHighlights;
    
    public PatchDocument WorkingCopyDocument { get; }

    public PatchDocument StageDocument { get; }

    public bool ShouldPerformIncrementalUpdate(
        GitIStagePatch workingCopyPatch,
        GitIStagePatch stagePatch,
        bool withHighlights)
    {
        if (!withHighlights || withHighlights && !HasHighlighting)
            return false;
        
        const int maxNumberOfChangesForIncrementalIUpdate = 3;
        var workingCopyChanges = GetCountOfChangedEntries(WorkingCopyDocument.Patch, workingCopyPatch);
        var stageChanges = GetCountOfChangedEntries(StageDocument.Patch, stagePatch);
        
        return workingCopyChanges <= maxNumberOfChangesForIncrementalIUpdate &&
               stageChanges <= maxNumberOfChangesForIncrementalIUpdate;
        
        static int GetCountOfChangedEntries(GitIStagePatch oldPatch, GitIStagePatch newPatch)
        {
            var oldEntries = PatchEntryKey.GetSet(oldPatch);
            return newPatch.Entries.Count(e => !oldEntries.Contains(e));
        }
    }

    public static DocumentState Create(
        GitIStagePatch workingCopyPatch,
        GitIStagePatch stagePatch)
    {
        return Create(null, null, workingCopyPatch, stagePatch, null);
    }

    public static DocumentState CreateHighlighted(
        GitIStagePatch workingCopyPatch,
        GitIStagePatch stagePatch,
        SyntaxTheme? theme)
    {
        return Create(null, null, workingCopyPatch, stagePatch, theme);
    }

    public DocumentState IncrementalUpdate(
        GitIStagePatch workingCopyPatch,
        GitIStagePatch stagePatch,
        SyntaxTheme theme)
    {
        return Create(WorkingCopyDocument, StageDocument, workingCopyPatch, stagePatch, theme);
    }

    public DocumentState DropHighlights()
    {
        var workingCopyDocument = new PatchDocument(WorkingCopyDocument.Patch, NoHighlights);
        var stageDocument = new PatchDocument(StageDocument.Patch, NoHighlights);
        return new DocumentState(workingCopyDocument, stageDocument);
    }
    
    private static DocumentState Create(
        PatchDocument? oldWorkingCopyDocument,
        PatchDocument? oldStageDocument,
        GitIStagePatch workingCopyPatch,
        GitIStagePatch stagePatch,
        SyntaxTheme? theme)
    {
        if (theme is null)
        {
            var workingCopyWithoutHighlights = new PatchDocument(workingCopyPatch, NoHighlights);
            var stageWithoutHighlights = new PatchDocument(stagePatch, NoHighlights);
            return new DocumentState( workingCopyWithoutHighlights, stageWithoutHighlights);
        }
        
        var workingCopyEntries = workingCopyPatch.Entries.ToDictionary(e => e.NewPath);
        var stageEntries = stagePatch.Entries.ToDictionary(e => e.NewPath);
        var files = new SortedSet<string>(workingCopyEntries.Keys.Concat(stageEntries.Keys));

        var workingCopyHighlightsByEntry = new Dictionary<PatchEntry, LineHighlights>();
        AddUnchangedEntries(workingCopyHighlightsByEntry, oldWorkingCopyDocument, workingCopyPatch);

        var stageHighlightsByEntry = new Dictionary<PatchEntry, LineHighlights>();
        AddUnchangedEntries(stageHighlightsByEntry, oldStageDocument, stagePatch);

#if DEBUG
        var repoPath = Repository.Discover(Environment.CurrentDirectory);
        using var repo = new Repository(repoPath);
#endif

        foreach (var file in files)
        {
            var workingCopyEntry = workingCopyEntries.GetValueOrDefault(file);
            var stageEntry = stageEntries.GetValueOrDefault(file);
            
            var workingCopyNeedsHighlighting =
                workingCopyEntry is not null &&
                !workingCopyHighlightsByEntry.ContainsKey(workingCopyEntry);

            var stageNeedsHighlighting =
                stageEntry is not null &&
                !stageHighlightsByEntry.ContainsKey(stageEntry);

            if (!workingCopyNeedsHighlighting && !stageNeedsHighlighting)
                continue;
            
            var workingCopyLines = TextLines.FromFile(file);
            var stageLines = workingCopyLines.ApplyReversed(workingCopyEntry);
            var committedLines = stageLines.ApplyReversed(stageEntry);

#if DEBUG
            var gitStageLines = GetStagedLines(repo, file);
            var gitCommittedLines = GetCommitedLines(repo, file);

            if (!stageLines.SequenceEqual(gitStageLines))
                throw new Exception($"Staged lines are wrong for '{file}'");
            
            if (!committedLines.SequenceEqual(gitCommittedLines))
                throw new Exception($"Committed lines are wrong for '{file}'");
#endif

            var grammar = theme.GetGrammar(file);
            GetPatchEntryWithHighlights(grammar, workingCopyHighlightsByEntry, workingCopyEntry, stageLines);
            GetPatchEntryWithHighlights(grammar, stageHighlightsByEntry, stageEntry, committedLines);
        }

        var workingCopyDocument = new PatchDocument(workingCopyPatch, workingCopyHighlightsByEntry.ToFrozenDictionary());
        var stageDocument = new PatchDocument(stagePatch, stageHighlightsByEntry.ToFrozenDictionary());
        return new DocumentState(workingCopyDocument, stageDocument);
        
        static void GetPatchEntryWithHighlights(
            IGrammar? grammar,
            Dictionary<PatchEntry, LineHighlights> receiver,
            PatchEntry? patchEntry,
            TextLines original)
        {
            if (patchEntry is null || grammar is null)
                return;

            var originalWithStates = TextLinesWithStates.Create(original, grammar);
            var entryWithHighlights = PatchEntryWithHighlights.Create(originalWithStates, patchEntry, grammar);
            receiver.Add(patchEntry, entryWithHighlights.LineHighlights);
        }
    }

    private static void AddUnchangedEntries(Dictionary<PatchEntry, LineHighlights> highlightsByEntry, PatchDocument? oldDocument, GitIStagePatch patch)
    {
        if (oldDocument is null || oldDocument.Highlights == NoHighlights)
            return;
        
        var newEntries = PatchEntryKey.GetSet(patch);
        var unchangedEntries = oldDocument.Patch.Entries.Where(e => newEntries.Contains(e));

        foreach (var unchangedEntry in unchangedEntries)
        {
            var oldHighlights = oldDocument.Highlights[unchangedEntry];
            highlightsByEntry.Add(unchangedEntry, oldHighlights);
        }
    }
#if DEBUG
    private static TextLines GetCommitedLines(Repository r, string path)
    {
        var entry = r.Head.Tip[path];
        if (entry is null)
            return TextLines.Empty;
        
        var blob = (Blob)entry.Target;
        using var stream = blob.GetContentStream();
        return TextLines.FromStream(stream);
    }
    
    private static TextLines GetStagedLines(Repository r, string path)
    {
        var indexEntry = r.Index[path];
        if (indexEntry is null)
            return TextLines.Empty;

        var blob = r.Lookup<Blob>(indexEntry.Id);
        using var stream = blob.GetContentStream();
        return TextLines.FromStream(stream);
    }
#endif
}

internal readonly struct PatchEntryKey : IEquatable<PatchEntryKey>
{
    private readonly PatchEntry _entry;
    private readonly int _hashCode;

    public PatchEntryKey(PatchEntry entry)
    {
        var span = GetSpan(entry);
        var spanBytes = MemoryMarshal.AsBytes(span);
        var hashCode = new HashCode();
        hashCode.AddBytes(spanBytes);
        
        _entry = entry;
        _hashCode = hashCode.ToHashCode();
    }

    private static ReadOnlySpan<char> GetSpan(PatchEntry entry)
    {
        return entry.Root.Text.AsSpan(entry.Span);
    }

    public bool Equals(PatchEntryKey other)
    {
        if (_hashCode != other._hashCode)
            return false;

        var span = GetSpan(_entry);
        var otherSpan = GetSpan(other._entry);
        return span.SequenceEqual(otherSpan);
    }

    public override bool Equals(object? obj)
    {
        return obj is PatchEntryKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _hashCode;
    }

    public static bool operator ==(PatchEntryKey left, PatchEntryKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PatchEntryKey left, PatchEntryKey right)
    {
        return !left.Equals(right);
    }
    
    public static implicit operator PatchEntryKey(PatchEntry entry)
    {
        return new PatchEntryKey(entry);
    }

    public static implicit operator PatchEntry(PatchEntryKey key)
    {
        return key._entry;
    }

    public static HashSet<PatchEntryKey> GetSet(GitIStagePatch patch)
    {
        var result = new HashSet<PatchEntryKey>();
        foreach (var entry in patch.Entries)
            result.Add(entry);
        return result;
    }
}
