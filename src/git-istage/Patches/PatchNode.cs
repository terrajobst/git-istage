using GitIStage.Text;

namespace GitIStage.Patches;

public abstract class PatchNode
{
    public abstract PatchNodeKind Kind { get; }

    public abstract Patch Root { get; }

    public PatchNode? Parent => Root.GetParent(this);

    public virtual TextSpan Span
    {
        get
        {
            var first = Children().First().Span;
            var last = Children().Last().Span;
            return TextSpan.FromBounds(first.Start, last.End);
        }
    }

    public virtual TextSpan FullSpan
    {
        get
        {
            var first = Children().First().FullSpan;
            var last = Children().Last().FullSpan;
            return TextSpan.FromBounds(first.Start, last.End);
        }
    }

    public abstract IEnumerable<PatchNode> Children();

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
        var queue = new Stack<PatchNode>();
        queue.Push(this);

        while (queue.Count > 0)
        {
            var current = queue.Pop();
            yield return current;
            foreach (var child in current.Children().Reverse())
                queue.Push(child);
        }
    }

    public IEnumerable<PatchNode> Descendants() => DescendantsAndSelf().Skip(1);

    public IEnumerable<PatchLine> GetLines() => DescendantsAndSelf().OfType<PatchLine>();
}