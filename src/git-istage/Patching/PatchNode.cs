using System.Diagnostics;
using GitIStage.Patching.Text;

namespace GitIStage.Patching;

public abstract class PatchNode
{
    public abstract PatchNodeKind Kind { get; }

    public virtual TextSpan Span
    {
        get
        {
            Debug.Assert(Children.Any());

            var start = int.MaxValue;
            var end = 0;

            foreach (var child in Children)
            {
                start = int.Min(start, child.Span.Start);
                end = int.Max(end, child.Span.End);
            }

            return TextSpan.FromBounds(start, end);
        }
    }

    public abstract IEnumerable<PatchNode> Children { get; }
}
