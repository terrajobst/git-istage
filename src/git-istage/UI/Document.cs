using System.Collections.Immutable;
using System.Diagnostics;
using GitIStage.Text;

namespace GitIStage.UI;

internal abstract class Document
{
    public static readonly Document Empty = new EmptyDocument();

    private StyledSpanTree? _spanTree;
    
    protected Document(SourceText sourceText)
    {
        ThrowIfNull(sourceText);

        SourceText = sourceText;
        Width = sourceText.Lines
            .Select(l => l.Text.AsSpan(l.Span).LengthVisual())
            .DefaultIfEmpty(0)
            .Max();
    }

    public bool IsEmpty => SourceText.Length == 0;

    public SourceText SourceText { get; }

    public int Height => SourceText.Lines.Length;

    public int Width { get; }

    public ReadOnlySpan<char> GetLine(int index)
    {
        var lineSpan = SourceText.Lines[index].Span;
        return SourceText.AsSpan().Slice(lineSpan);
    }

    public IEnumerable<StyledSpan> GetLineStyles(int index)
    {
        if (_spanTree is null)
            yield break;
        
        var lineSpans = new List<StyledSpan>();
        GetLineStyles(index, lineSpans);

        // NOTE: This method is expected to return line relative spans.
        var line = SourceText.Lines[index];
        var lineStart = line.Span.Start;
        
        foreach (var styledSpan in lineSpans)
        {
            Debug.Assert(line.Span.Contains(styledSpan.Span));

            var span = styledSpan.Span;
            var lineRelativeSpan = new TextSpan(span.Start - lineStart, span.Length);
            yield return new StyledSpan(lineRelativeSpan, styledSpan.Style);
        }
    }

    public void GetLineStyles(int index, List<StyledSpan> receiver)
    {
        var lineSpan = SourceText.Lines[index].Span;
        GetStyles(lineSpan, receiver);
    }

    public void GetStyles(TextSpan span, List<StyledSpan> receiver)
    {
        if (_spanTree is null)
            return;
        
        _spanTree.FindSpans(span, receiver);
    }

    protected async void LoadStyles()
    {
        // TODO: Not sure why doing a sync operation causes a hang.
        
        if (Program._application is null)
        {
            var styles = await Task.Run(GetStyles);
            _spanTree = new StyledSpanTree(styles);

            // HACK: Ideally we'd raise an event here
            Program.Render();
        }
        else
        {
            _spanTree = new StyledSpanTree(GetStyles());
        }
    }

    protected virtual IEnumerable<StyledSpan> GetStyles()
    {
        return [];
    }

    private sealed class EmptyDocument() : Document(SourceText.Empty);
}