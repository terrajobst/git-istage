namespace GitIStage.UI;

internal sealed class SearchHit
{
    public SearchHit(int lineIndex, int start, int length)
    {
        LineIndex = lineIndex;
        Start = start;
        Length = length;
    }

    public int LineIndex { get; }
    public int Start { get; }
    public int Length { get; }
}