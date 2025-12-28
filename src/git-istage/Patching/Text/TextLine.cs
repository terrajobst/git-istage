namespace GitIStage.Patching.Text;

public sealed class TextLine
{
    public TextLine(SourceText text, int start, int length, int lengthIncludingLineBreak)
    {
        ThrowIfNull(text);
        ThrowIfNegative(start);
        ThrowIfNegative(length);
        ThrowIfLessThan(lengthIncludingLineBreak, length);

        Text = text;
        Start = start;
        Length = length;
        LengthIncludingLineBreak = lengthIncludingLineBreak;
    }

    public SourceText Text { get; }
    public int LineIndex => Text.GetLineIndex(Span.Start);
    public int Start { get; }
    public int Length { get; }
    public int End => Start + Length;
    public int LengthIncludingLineBreak { get; }
    public TextSpan Span => new TextSpan(Start, Length);
    public TextSpan SpanIncludingLineBreak => new TextSpan(Start, LengthIncludingLineBreak);
    public TextSpan LineBreakSpan => TextSpan.FromBounds(Span.End, SpanIncludingLineBreak.End); 
    
    public override string ToString() => Text.ToString(Span);
}