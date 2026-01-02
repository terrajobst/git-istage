namespace GitIStage.Patches.EntryHeaders;

public sealed class CopyToHeader : PatchEntryAdditionalHeader
{
    internal CopyToHeader(Patch root,
                          PatchToken copyKeyword,
                          PatchToken toKeyword,
                          PatchToken<string> path)
        : base(root)
    {
        CopyKeyword = copyKeyword;
        ToKeyword = toKeyword;
        Path = path;
    }

    public override PatchNodeKind Kind => PatchNodeKind.CopyToHeader;

    public PatchToken CopyKeyword { get; }

    public PatchToken ToKeyword { get; }

    public PatchToken<string> Path { get; }

    public override IEnumerable<PatchNode> Children() =>
    [
        CopyKeyword,
        ToKeyword,
        Path
    ];
}