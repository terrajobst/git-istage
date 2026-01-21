using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using GitIStage.Patches;
using GitIStage.Services;
using GitIStage.Text;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace GitIStage.UI;

internal sealed class PatchDocument : Document
{
    public PatchDocument(Patch patch, FrozenDictionary<PatchEntry, LineHighlights> highlights)
        : base(patch.Text)
    {
        Patch = patch;
        Highlights = highlights;
    }

    public Patch Patch { get; }

    public FrozenDictionary<PatchEntry, LineHighlights> Highlights { get; }

    public override TextStyle GetLineStyle(int index)
    {
        var line = Patch.Lines[index];
        var lineForeground = GetLineForegroundColor(line);
        var lineBackground = lineForeground?.Lerp(TextColor.Black, 0.8f);

        var hasSyntaxColoring = line is PatchHunkLine hunkLine &&
                                Highlights.ContainsKey(hunkLine.Ancestors().OfType<PatchEntry>().First());
        
        return new TextStyle
        {
            Foreground = hasSyntaxColoring ? null : lineForeground,
            Background = lineBackground
        };
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

    public override void GetLineStyles(int index, List<StyledSpan> receiver)
    {
        var line = Patch.Lines[index];
        if (line is PatchHunkLine hunkLine)
        {
            var entry = hunkLine.Ancestors().OfType<PatchEntry>().First();
            var startLine = entry.Hunks.First().Lines.First().LineIndex;

            if (hunkLine.Span.Length > 0)
            {
                var lineForeground = GetLineForegroundColor(hunkLine);
                var modifierStyle = new TextStyle { Foreground = lineForeground };
                receiver.Add(new StyledSpan(new TextSpan(0, 1), modifierStyle));
            }
            
            if (Highlights.TryGetValue(entry, out var lineHighlights))
            {
                var highlightLineIndex = index - startLine;
                if (highlightLineIndex >= 0 && highlightLineIndex < lineHighlights.Count)
                    receiver.AddRange(lineHighlights[highlightLineIndex].AsSpan());
            }
        }
        else
        {
            var styledTokens = line
                .Descendants()
                .OfType<PatchToken>()
                .Select(ToStyledSpan);
            receiver.AddRange(styledTokens);
        }
    }

    private static StyledSpan ToStyledSpan(PatchToken token)
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

        var text = token.Root.Text;
        var lineIndex = text.GetLineIndex(token.Span.Start);
        var lineStart = text.Lines[lineIndex].Start;
        return new StyledSpan(token.Span.RelativeTo(lineStart), foreground, null);
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
            lock (theme._registry)
            {
                var initialScopeName = theme._options.GetScopeByExtension(ext);
                return theme._registry.LoadGrammar(initialScopeName);                
            }
        }, this);
    }
}