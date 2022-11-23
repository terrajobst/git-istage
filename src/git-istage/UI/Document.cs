namespace GitIStage.UI;

internal abstract class Document
{
    public static readonly Document Empty = new EmptyDocument();

    public abstract int Height { get; }
    public abstract int Width { get; }

    public abstract string GetLine(int index);

    public int GetLineIndex(string line)
    {
        for (var i = 0; i < Height; i++)
        {
            var l = GetLine(i);
            if (l == line)
                return i;
        }

        return -1;
    }

    private sealed class EmptyDocument : Document
    {
        public override int Height => 0;
        public override int Width => 0;

        public override string GetLine(int index)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}