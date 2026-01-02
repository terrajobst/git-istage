using System.Collections.Immutable;
using GitIStage.Text;

namespace GitIStage.Patches;

public sealed class PatchHunk : PatchNode
{
    internal PatchHunk(Patch root,
                       PatchHunkHeader header,
                       ImmutableArray<PatchHunkLine> lines)
    {
        ThrowIfNull(root);
        ThrowIfNull(header);

        Root = root;
        Header = header;
        Lines = lines;
    }

    public override PatchNodeKind Kind => PatchNodeKind.Hunk;

    public override Patch Root { get; }

    public PatchHunkHeader Header { get; }

    public ImmutableArray<PatchHunkLine> Lines { get; }

    public LineRange OldRange => Header.OldRange.Value;

    public LineRange NewRange => Header.NewRange.Value;

    public override IEnumerable<PatchNode> Children() => [Header, ..Lines];
}