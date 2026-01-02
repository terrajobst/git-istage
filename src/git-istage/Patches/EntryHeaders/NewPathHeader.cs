namespace GitIStage.Patches.EntryHeaders;

public sealed class NewPathHeader : PatchEntryAdditionalHeader
{
    internal NewPathHeader(Patch root,
                           PatchToken plusPlusPlusToken,
                           PatchToken<string> path)
        : base(root)
    {
        PlusPlusPlusToken = plusPlusPlusToken;
        Path = path;
    }

    public override PatchNodeKind Kind => PatchNodeKind.NewPathHeader;

    public PatchToken PlusPlusPlusToken { get; }

    public PatchToken<string> Path { get; }

    public override IEnumerable<PatchNode> Children() =>
    [
        PlusPlusPlusToken,
        Path
    ];
}