using GitIStage.Text;

namespace GitIStage.UI;

internal abstract class Document
{
    public static readonly Document Empty = new EmptyDocument();

    public abstract int Height { get; }
    public abstract int Width { get; }
    public abstract int EntryCount { get; }

    public abstract string GetLine(int index);
    public abstract int GetLineIndex(int index);
    public abstract int FindEntryIndex(int lineIndex);

    public virtual IEnumerable<StyledSpan> GetLineStyles(int index) => [];
    
    private sealed class EmptyDocument : Document
    {
        public override int Height => 0;
        public override int Width => 0;
        public override int EntryCount => 0;

        public override string GetLine(int index)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public override int GetLineIndex(int index)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public override int FindEntryIndex(int lineIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(lineIndex));
        }
    }
}