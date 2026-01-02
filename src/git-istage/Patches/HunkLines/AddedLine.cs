namespace GitIStage.Patches.HunkLines;

public sealed class AddedLine : PatchHunkLine
{
    internal AddedLine(Patch root,
                       PatchToken marker,
                       PatchToken text)
        : base(root)
    {
        Marker = marker;
        Text = text;
    }

    public override PatchNodeKind Kind => PatchNodeKind.AddedLine;

    public PatchToken Marker { get; }

    public new PatchToken Text { get; }

    public override IEnumerable<PatchNode> Children() =>
    [
        Marker,
        Text
    ];
}