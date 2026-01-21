using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using GitIStage.Patches;
using GitIStage.Services;
using GitIStage.Text;
using Microsoft.Extensions.DependencyInjection;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace GitIStage.UI;

internal sealed class PatchDocument : Document
{
    private readonly bool _isWorkingCopy;
    private readonly PatchHighlighterService _patchHighlighterService;

    private PatchDocument(Patch patch, bool isWorkingCopy, PatchHighlighterService patchHighlighterService)
        : base(patch.Text)
    {
        ThrowIfNull(patch);
        ThrowIfNull(patchHighlighterService);

        _isWorkingCopy = isWorkingCopy;
        _patchHighlighterService = patchHighlighterService;

        Patch = patch;
        LoadStyles();
    }

    public Patch Patch { get; }

    protected override IEnumerable<StyledSpan> GetStyles()
    {
        var styles = new List<StyledSpan>();

        foreach (var entry in Patch.Entries)
        {
            var allTokensExceptForDiffLines = entry
                .Descendants()
                .Where(n => n is PatchLine && n is not PatchHunkLine)
                .SelectMany(h => h.DescendantsAndSelf())
                .OfType<PatchToken>()
                .Select(GetSpan);

            foreach (var styledToken in allTokensExceptForDiffLines)
                styles.Add(styledToken);

            var patchHighlighter = _patchHighlighterService.GetHighlighter(entry.NewPath);
            patchHighlighter.GetHighlights(styles, entry);
        }

        return styles;
    }

    public static PatchDocument Create(Patch patch, bool isWorkingCopy, PatchHighlighterService patchHighlighterService)
    {
        return new PatchDocument(patch, isWorkingCopy, patchHighlighterService);
    }

    private static StyledSpan GetSpan(PatchToken token)
    {
        var foreground = token.Kind switch
        {
            PatchNodeKind.PathToken => Colors.PathTokenForeground,
            PatchNodeKind.HashToken => Colors.HashTokenForeground,
            PatchNodeKind.ModeToken => Colors.ModeTokenForeground,
            PatchNodeKind.TextToken => Colors.TextTokenForeground,
            PatchNodeKind.PercentageToken => Colors.PercentageTokenForeground,
            PatchNodeKind.RangeToken => Colors.RangeTokenForeground,
            PatchNodeKind.MinusMinusMinusToken => Colors.MinusMinusMinusTokenForeground,
            PatchNodeKind.PlusPlusPlusToken => Colors.PlusPlusPlusTokenForeground,
            _ => token.Kind.IsKeyword()
                ? Colors.KeywordForeground
                : token.Kind.IsOperator()
                    ? Colors.OperatorForeground
                    : throw new UnreachableException($"Unexpected token kind {token.Kind}")
        };

        return new StyledSpan(token.Span, foreground, null);
    }
}

internal sealed class SyntaxTheme
{
    public static SyntaxTheme Instance { get; } = new();

    private readonly RegistryOptions _options;
    private readonly Registry _registry;
    private readonly Theme _theme;
    private readonly ConcurrentDictionary<string, IGrammar?> _grammarByExtensions = new();

    private SyntaxTheme()
    {
        _options = new RegistryOptions(ThemeName.DarkPlus);
        _registry = new Registry(_options);
        _theme = _registry.GetTheme();
    }

    public Theme Theme => _theme;

    public IGrammar? GetGrammar(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return _grammarByExtensions.GetOrAdd(extension, static (ext, theme) =>
        {
            var initialScopeName = theme._options.GetScopeByExtension(ext);
            return theme._registry.LoadGrammar(initialScopeName);
        }, this);
    }
}

public abstract class PatchHighlighter
{
    public static PatchHighlighter None => NoHighlights.Instance;

    private PatchHighlighter()
    {
    }

    public abstract void GetHighlights(List<StyledSpan> receiver, PatchEntry patch);

    public static PatchHighlighter Create(SourceText workingCopyContents,
                                          PatchEntry patchEntry,
                                          bool performSyntaxHighlighting = true)
    {
        ThrowIfNull(workingCopyContents);
        ThrowIfNull(patchEntry);

        if (performSyntaxHighlighting)
        {
            var grammar = SyntaxTheme.Instance.GetGrammar(workingCopyContents.FileName);
            if (grammar is not null)
                return Highlights.Create(grammar, workingCopyContents, patchEntry);
        }

        return NoHighlights.Instance;
    }

    private sealed class NoHighlights : PatchHighlighter
    {
        public static NoHighlights Instance { get; } = new();

        private NoHighlights()
        {
        }

        public override void GetHighlights(List<StyledSpan> receiver, PatchEntry patch)
        {
            foreach (var hunk in patch.Hunks)
            {
                foreach (var hunkLine in hunk.Lines)
                {
                    var lineForeground = GetLineForegroundColor(hunkLine);
                    var lineBackground = lineForeground?.Lerp(TextColor.Black, 0.7f);
                    var lineStyle = new TextStyle { Foreground = lineForeground, Background = lineBackground };
                    if (hunkLine.Span.Length > 0)
                        receiver.Add(new StyledSpan(new TextSpan(hunkLine.Span.Start, 1), lineStyle));
                }
            }

        }
    }

    private sealed class Highlights : PatchHighlighter
    {
        private readonly IGrammar _grammar;
        private readonly SourceText _oldText;
        private readonly ImmutableArray<IStateStack?> _oldLineStates;

        private Highlights(IGrammar grammar, SourceText oldText, ImmutableArray<IStateStack?> oldLineStates)
        {
            _grammar = grammar;
            _oldText = oldText;
            _oldLineStates = oldLineStates;
        }

        public static Highlights Create(IGrammar grammar,
                                        SourceText workingCopyContents,
                                        PatchEntry patch)
        {
            var oldLineStateBuilder = ImmutableArray.CreateBuilder<IStateStack?>();

            // We apply the patch in reverse to workingCopyContents. This gives us
            // the old (committed) state.
            var workingCopyLine = 0;
            var oldTextBuilder = new StringBuilder();
            IStateStack? oldState = null;
            oldLineStateBuilder.Add(oldState);

            var text = patch.Root.Text;

            foreach (var hunk in patch.Hunks)
            {
                // First we need to highlight all the lines between the last
                // hunk and this hunk.
                //
                // By definition these are unchanged lines, hence they are part
                // of the committed state. But we still need to use the newLine
                // number because we index into the working copy which is the
                // committed state plus the patch.
                var newLine = hunk.NewRange.LineNumber - 1;
                HighlightRange(ref workingCopyLine, newLine, ref oldState, oldTextBuilder);

                foreach (var line in hunk.Lines)
                {
                    var lineStart = line.Span.Start + 1;
                    var lineSpan = TextSpan.FromBounds(lineStart, line.Span.End);
                    var lineText = text.AsMemory(lineSpan);
                    var lineSpanWithLineBreak = TextSpan.FromBounds(lineStart, line.FullSpan.End);
                    var lineTextWithLineBreak = text.AsMemory(lineSpanWithLineBreak);

                    if (line.Kind == PatchNodeKind.ContextLine)
                    {
                        oldTextBuilder.Append(lineTextWithLineBreak);
                        GetHighlights(grammar, lineText, ref oldState);
                        oldLineStateBuilder.Add(oldState);
                        workingCopyLine++;
                    }
                    else if (line.Kind == PatchNodeKind.AddedLine)
                    {
                        // Ignore
                        workingCopyLine++;
                    }
                    else if (line.Kind == PatchNodeKind.DeletedLine)
                    {
                        oldTextBuilder.Append(lineTextWithLineBreak);
                        GetHighlights(grammar, lineText, ref oldState);
                        oldLineStateBuilder.Add(oldState);
                    }
                }
            }

            HighlightRange(ref workingCopyLine, workingCopyContents.Lines.Length, ref oldState, oldTextBuilder);
            var oldText = SourceText.From(oldTextBuilder.ToString());

#if DEBUG
            AssertMatch(patch.OldPath, grammar, oldLineStateBuilder, oldText);
#endif

            return new Highlights(grammar, oldText, oldLineStateBuilder.ToImmutable());

            void HighlightRange(ref int startLine, int endLine, ref IStateStack? state, StringBuilder oldTextBuilder)
            {
                for (var i = startLine; i < endLine; i++)
                {
                    var line = workingCopyContents.Lines[i];
                    var lineText = workingCopyContents.AsMemory(line.Span);
                    oldTextBuilder.Append(line.Text.AsSpan(line.SpanIncludingLineBreak));
                    GetHighlights(grammar, lineText, ref state);
                    oldLineStateBuilder.Add(state);
                }

                startLine = endLine;
            }
#if DEBUG
            void AssertMatch(string fileName, IGrammar grammar, IList<IStateStack?> expectedStates, SourceText oldText)
            {
                var current = Environment.CurrentDirectory;
                var repoPath = LibGit2Sharp.Repository.Discover(current);
                using var repo = new LibGit2Sharp.Repository(repoPath);
                var blobEntry = repo.Head.Tip[fileName];
                if (blobEntry is null)
                {
                    Debug.Assert(expectedStates.Count == 1);
                    Debug.Assert(oldText.Lines.Length == 0);
                    return;
                }

                var blob = blobEntry.Target as LibGit2Sharp.Blob;
                Debug.Assert(blob is not null);
                using var content = new StreamReader(blob.GetContentStream());
                var committedContent = content.ReadToEnd();
                var committedText = SourceText.From(committedContent);

                Debug.Assert(committedText.Lines.Length == oldText.Lines.Length);

                var actualStates = new List<IStateStack?>();
                var state = (IStateStack?)null;
                actualStates.Add(state);
                foreach (var (line, oldLine) in committedText.Lines.Zip(oldText.Lines))
                {
                    var oldLineText = oldText.AsSpan(oldLine.Span);
                    var committedLineText = committedText.AsSpan(line.Span);
                    Debug.Assert(oldLineText.SequenceEqual(committedLineText));

                    var lineText = committedText.AsMemory(line.Span);
                    GetHighlights(grammar, lineText, ref state);
                    actualStates.Add(state);
                }

                Debug.Assert(actualStates.Count == expectedStates.Count);
                for (var i = 0; i < actualStates.Count; i++)
                {
                    var actualState = (StateStack?) actualStates[i];
                    var expectedState = (StateStack?) expectedStates[i];

                    if (actualState is null && expectedState is null)
                        continue;

                    Debug.Assert(SameState(actualState, expectedState), "States do not match at line {i + 1}");
                }
            }
#endif
        }

        private static bool SameState(IStateStack? a, IStateStack? b)
        {
            if (a is null && b is null)
                return true;

            if (a is null || b is null)
                return false;

            var stateA = (StateStack)a;
            var stateB = (StateStack)b;

            // public AttributedScopeStack NameScopesList { get; private set; }
            // public AttributedScopeStack ContentNameScopesList { get; private set; }

            return stateA.RuleId == stateB.RuleId &&
                   stateA.Depth == stateB.Depth &&
                   stateA.EndRule == stateB.EndRule &&
                   stateA.BeginRuleCapturedEOL == stateB.BeginRuleCapturedEOL &&
                   SameState(stateA.Parent, stateB.Parent);
        }

        public override void GetHighlights(List<StyledSpan> receiver, PatchEntry patch)
        {
            var text = patch.Root.Text;
            var reHighlightOldLineStart = -1;
            var newState = (IStateStack?)null;

            foreach (var hunk in patch.Hunks)
            {
                // old states will always have at least one line. And the first
                // state is always null, hence we can fold -1 and 0 both to 0.
                var oldLine = int.Max(0, hunk.OldRange.LineNumber - 1);
                var newLine = hunk.NewRange.LineNumber - 1;

                if (reHighlightOldLineStart < 0)
                {
                    newState = _oldLineStates[oldLine];
                }
                else
                {
                    Debug.Assert(newState is not null);

                    for (var line = reHighlightOldLineStart; line < oldLine; line++)
                    {
                        var oldLineMemory = _oldText.AsMemory(_oldText.Lines[line].Span);
                        GetHighlights(_grammar, oldLineMemory, ref newState);
                    }
                }

                foreach (var hunkLine in hunk.Lines)
                {
                    var lineForeground = GetLineForegroundColor(hunkLine);
                    var lineBackground = lineForeground?.Lerp(TextColor.Black, 0.7f);
                    var lineStyle = new TextStyle { Background = lineBackground };
                    if (hunkLine.Span.Length > 0)
                    {
                        var modifierStyle = new TextStyle { Foreground = lineForeground, Background = lineBackground };
                        receiver.Add(new StyledSpan(new TextSpan(hunkLine.Span.Start, 1), modifierStyle));
                    }

                    var lineStart = hunkLine.Span.Start + 1;
                    var lineSpan = TextSpan.FromBounds(lineStart, hunkLine.Span.End);
                    var lineText = text.AsMemory(lineSpan);

                    if (hunkLine.Kind == PatchNodeKind.ContextLine)
                    {
                        GetHighlights(_grammar, lineStart, lineText, ref newState, lineStyle, receiver);
                        oldLine++;
                        newLine++;
                    }
                    else if (hunkLine.Kind == PatchNodeKind.AddedLine)
                    {
                        GetHighlights(_grammar, lineStart, lineText, ref newState, lineStyle, receiver);
                        newLine++;
                    }
                    else if (hunkLine.Kind == PatchNodeKind.DeletedLine)
                    {
                        GetHighlights(_grammar, lineStart, lineText, _oldLineStates[oldLine], lineStyle, receiver);
                        oldLine++;
                    }
                }

                reHighlightOldLineStart = SameState(newState, _oldLineStates[oldLine]) ? -1 : oldLine;
            }
        }

        private static void GetHighlights(IGrammar grammar, int lineStart, ReadOnlyMemory<char> text, IStateStack? state, TextStyle lineStyle, ICollection<StyledSpan> receiver)
        {
            GetHighlights(grammar, lineStart, text, ref state, lineStyle, receiver);
        }

        private static void GetHighlights(IGrammar grammar, int lineStart, ReadOnlyMemory<char> text, ref IStateStack? state, TextStyle lineStyle, ICollection<StyledSpan> receiver)
        {
            var tokenizedLine = grammar.TokenizeLine(text, state, TimeSpan.MaxValue);
            state = tokenizedLine.RuleStack;

            if (receiver is null)
                return;

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

                style = style.PlaceOnTopOf(lineStyle);

                var span = new TextSpan(lineStart + startIndex, endIndex - startIndex);
                var styledSpan = new StyledSpan(span, style);
                receiver.Add(styledSpan);
            }
        }

        private static void GetHighlights(IGrammar grammar, ReadOnlyMemory<char> text, ref IStateStack? state)
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

    private static TextColor? GetLineForegroundColor(PatchLine line)
    {
        switch (line.Kind)
        {
            case PatchNodeKind.EntryHeader:
                return Colors.EntryHeaderForeground;
            case PatchNodeKind.AddedLine:
                return Colors.AddedText;
            case PatchNodeKind.DeletedLine:
                return Colors.DeletedText;
            default:
                return null;
        }
    }
}

internal sealed class PatchHighlighterService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, PatchHighlighter> _highlighters = new();

    public PatchHighlighterService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Clear()
    {
        _highlighters.Clear();
    }

    public PatchHighlighter GetHighlighter(string fileName)
    {
        return _highlighters.GetOrAdd(fileName, CreateHighlighter, _serviceProvider);

        static PatchHighlighter CreateHighlighter(string fileName, IServiceProvider serviceProvider)
        {
            var documentService = serviceProvider.GetRequiredService<DocumentService>();
            var workingCopyPatchEntry = documentService.WorkingCopyPatch.Entries.SingleOrDefault(e => string.Equals(e.NewPath, fileName, StringComparison.Ordinal));
            var stagedPatchEntry = documentService.WorkingCopyPatch.Entries.SingleOrDefault(e => string.Equals(e.NewPath, fileName, StringComparison.Ordinal));
            if (workingCopyPatchEntry is null && stagedPatchEntry is null)
                return PatchHighlighter.None;

            var patch = stagedPatchEntry ?? workingCopyPatchEntry!;

            var workingCopyContents = SourceText.Load(fileName);
            return PatchHighlighter.Create(workingCopyContents, patch, performSyntaxHighlighting: true);
        }
    }
}