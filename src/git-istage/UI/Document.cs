using GitIStage.Text;

namespace GitIStage.UI;

internal abstract class Document
{
    public static readonly Document Empty = new EmptyDocument();

    protected Document(SourceText sourceText)
    {
        ThrowIfNull(sourceText);

        SourceText = sourceText;

        // TODO: This is super inefficient.
        Width = sourceText.Lines.Select(l => l.Text.ToString())
            .DefaultIfEmpty(string.Empty)
            .Max(t => t.LengthVisual());
    }

    public SourceText SourceText { get; }
    
    public int Height => SourceText.Lines.Length;

    public int Width { get; }

    public ReadOnlySpan<char> GetLine(int index)
    {
        var lineSpan = SourceText.Lines[index].Span;
        return SourceText.AsSpan().Slice(lineSpan);
    }

    public virtual IEnumerable<StyledSpan> GetLineStyles(int index) => [];

    private sealed class EmptyDocument() : Document(SourceText.Empty);
}