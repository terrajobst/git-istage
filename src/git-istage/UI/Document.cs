using GitIStage.Services;
using GitIStage.Text;

namespace GitIStage.UI;

internal abstract class Document
{
    public static readonly Document Empty = new EmptyDocument();

    private LineHighlights? _lineHighlights;
    
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

    public virtual TextStyle GetLineStyle(int index)
    {
        return default;
    }
    
    public virtual void GetLineStyles(int index, List<StyledSpan> receiver)
    {
        if (_lineHighlights is null)
            _lineHighlights = GetLineHighlights();

        if (_lineHighlights == LineHighlights.Empty)
            return;

        receiver.AddRange(_lineHighlights[index].AsSpan());
    }

    protected virtual LineHighlights GetLineHighlights()
    {
        return LineHighlights.Empty;
    }

    private sealed class EmptyDocument() : Document(SourceText.Empty);
}