using GitIStage.Text;

namespace GitIStage.UI;

internal abstract class Document
{
    public static readonly Document Empty = new EmptyDocument();

    public abstract int Height { get; }

    public abstract int Width { get; }

    public abstract ReadOnlySpan<char> GetLine(int index);

    public virtual IEnumerable<StyledSpan> GetLineStyles(int index) => [];
    
    private sealed class EmptyDocument : Document
    {
        public override int Height => 0;

        public override int Width => 0;

        public override ReadOnlySpan<char> GetLine(int index)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}