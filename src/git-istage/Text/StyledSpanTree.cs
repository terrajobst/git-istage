using System.Collections;

namespace GitIStage.Text;

// Represents an AVL balanced, non-overlapping interval tree.
//
// We use this to combine overlapping styled spans into a single, non-overlapping sequence of styled spans.
internal sealed partial class StyledSpanTree : IEnumerable<StyledSpan>
{
    private StyledSpanNode? _root;

    public StyledSpanTree()
    {
    }

    public StyledSpanTree(IEnumerable<StyledSpan> spanStyles)
        : this()
    {
        foreach (var spanStyle in spanStyles)
            Insert(spanStyle);
    }

    public void Clear()
    {
        _root = null;
    }

    public void Insert(StyledSpan span)
    {
        if (span.Span.Length == 0)
            return;

        _root = Insert(_root, span);
    }

    private static StyledSpanNode Insert(StyledSpanNode? node, StyledSpan span)
    {
        if (node == null)
            return new StyledSpanNode(span);

        if (node.Span.OverlapsWith(span.Span))
            throw new ArgumentException("overlaps with an existing span.", nameof(span));

        // Check which side of the node the interval belongs in and recurse
        if (span.Span.Start < node.Span.Start)
        {
            node.Left = Insert(node.Left, span);
        }
        else if (span.Span.Start > node.Span.Start)
        {
            node.Right = Insert(node.Right, span);
        }
        else
        {
            throw new Exception("This code shouldn't be reachable");
        }

        // Update the height of the node
        node.Height = 1 + int.Max(node.Left?.Height ?? 0, node.Right?.Height ?? 0);

        // Check if the tree is unbalanced and perform a rotation if necessary
        var balance = GetBalance(node);

        switch (balance)
        {
            case > 1 when span.Span.Start < node.Left!.Span.Start:
                return RightRotate(node);
            case < -1 when span.Span.Start > node.Right!.Span.Start:
                return LeftRotate(node);
            case > 1 when span.Span.Start > node.Left.Span.Start:
                node.Left = LeftRotate(node.Left);
                return RightRotate(node);
            case < -1 when span.Span.Start < node.Right.Span.Start:
                node.Right = RightRotate(node.Right);
                return LeftRotate(node);
            default:
                return node;
        }
    }

    public void Remove(TextSpan span)
    {
        _root = Remove(_root, span);
    }

    private static StyledSpanNode? Remove(StyledSpanNode? node, TextSpan span)
    {
        if (node is null)
            return null;

        // Check which side of the node the interval belongs in and recurse
        if (span.Start < node.Span.Start)
        {
            node.Left = Remove(node.Left, span);
        }
        else if (span.Start > node.Span.Start)
        {
            node.Right = Remove(node.Right, span);
        }
        else
        {
            // If the start value is equal to the node's start value,
            // check if the end value is equal to the node's end value
            if (span.End != node.Span.End)
                return node;

            // If the start and end values are equal to the node's start and end values,
            // the node is found and can be removed from the tree
            if (node.Left is null || node.Right is null)
            {
                node = node.Left ?? node.Right;
            }
            else
            {
                var successor = FindMin(node.Right);
                node.StyledSpan = successor.StyledSpan;
                node.Right = Remove(node.Right, successor.Span);
            }
        }

        if (node is null)
            return null;

        node.Height = 1 + int.Max(node.Left?.Height ?? 0, node.Right?.Height ?? 0);

        var balance = GetBalance(node);

        switch (balance)
        {
            case > 1 when GetBalance(node.Left!) >= 0:
                return RightRotate(node);
            case > 1 when GetBalance(node.Left!) < 0:
                node.Left = LeftRotate(node.Left!);
                return RightRotate(node);
            case < -1 when GetBalance(node.Right!) <= 0:
                return LeftRotate(node);
            case < -1 when GetBalance(node.Right!) > 0:
                node.Right = RightRotate(node.Right!);
                return LeftRotate(node);
            default:
                return node;
        }
    }

    private static StyledSpanNode FindMin(StyledSpanNode node)
    {
        while (true)
        {
            if (node.Left is null)
                return node;

            node = node.Left;
        }
    }

    private static int GetBalance(StyledSpanNode node)
    {
        var leftHeight = node.Left?.Height ?? 0;
        var rightHeight = node.Right?.Height ?? 0;
        return leftHeight - rightHeight;
    }

    private static StyledSpanNode RightRotate(StyledSpanNode node)
    {
        var left = node.Left!;
        var leftRight = left.Right;

        left.Right = node;
        node.Left = leftRight;

        // Update the height of the nodes
        node.Height = 1 + int.Max(node.Left?.Height ?? 0, node.Right?.Height ?? 0);
        left.Height = 1 + int.Max(left.Left?.Height ?? 0, left.Right?.Height ?? 0);

        return left;
    }

    private static StyledSpanNode LeftRotate(StyledSpanNode node)
    {
        var right = node.Right!;
        var rightLeft = right.Left;

        right.Left = node;
        node.Right = rightLeft;

        // Update the height of the nodes
        node.Height = 1 + int.Max(node.Left?.Height ?? 0, node.Right?.Height ?? 0);
        right.Height = 1 + int.Max(right.Left?.Height ?? 0, right.Right?.Height ?? 0);

        return right;
    }

    public IEnumerator<StyledSpan> GetEnumerator()
    {
        var result = new List<StyledSpan>();
        InOrder(_root, result);
        return result.GetEnumerator();

        static void InOrder(StyledSpanNode? node, List<StyledSpan> result)
        {
            if (node is null)
                return;

            InOrder(node.Left, result);
            result.Add(node.StyledSpan);
            InOrder(node.Right, result);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void FindSpans(TextSpan span, List<StyledSpan> receiver)
    {
        FindSpans(_root, span, receiver);
    }

    private static void FindSpans(StyledSpanNode? node, TextSpan span, List<StyledSpan> receiver)
    {
        if (node is null)
            return;

        // Check which side of the node the given interval belongs in and recurse
        if (node.Left is not null && span.Start <= node.Span.Start)
            FindSpans(node.Left, span, receiver);

        // Check if the interval defined by the node overlaps with the given interval
        if (node.Span.OverlapsWith(span))
            receiver.Add(node.StyledSpan);

        if (node.Right is not null && span.End >= node.Span.End)
            FindSpans(node.Right, span, receiver);
    }
}