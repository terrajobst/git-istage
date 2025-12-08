using System.Collections.Immutable;
using GitIStage.Patching.Text;

namespace GitIStage.Patching;

public sealed class PatchHunk : PatchNode
{
    internal PatchHunk(PatchHunkHeader header,
                       IEnumerable<PatchHunkLine> lines)
    {
        ThrowIfNull(header);
        ThrowIfNull(lines);

        Header = header;
        Lines = [..lines];

        var everything = (PatchNode[])[Header, ..Lines];
        var start = everything.Min(n => n.Span.Start);
        var end = everything.Max(n => n.Span.End);
        Span = TextSpan.FromBounds(start, end);
    }

    public override PatchNodeKind Kind => PatchNodeKind.Hunk;

    public override TextSpan Span { get; }

    public PatchHunkHeader Header { get; }

    public ImmutableArray<PatchHunkLine> Lines { get; }

    public override IEnumerable<PatchNode> Children => [Header, ..Lines];
}