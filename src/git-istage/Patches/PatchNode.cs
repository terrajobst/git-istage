using System.Diagnostics;
using GitIStage.Text;

namespace GitIStage.Patches;

public abstract class PatchNode
{
    public abstract Patch Root { get; }

    public PatchNode? Parent => Root.GetParent(this);

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

    public IEnumerable<PatchNode> Ancestors() => AncestorsAndSelf().Skip(1);

    public IEnumerable<PatchNode> AncestorsAndSelf()
    {
        var current = this;
        while (current is not null)
        {
            yield return current;
            current = current.Parent;
        }
    }

    public IEnumerable<PatchNode> DescendantsAndSelf()
    {
        var queue = new Queue<PatchNode>();
        queue.Enqueue(this);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current;
            foreach (var child in current.Children)
                queue.Enqueue(child);
        }
    }

    public IEnumerable<PatchNode> Descendants() => DescendantsAndSelf().Skip(1);

    public IEnumerable<PatchLine> GetLines() => DescendantsAndSelf().OfType<PatchLine>();
}
