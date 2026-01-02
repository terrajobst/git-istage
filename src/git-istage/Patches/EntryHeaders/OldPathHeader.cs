namespace GitIStage.Patches.EntryHeaders;

public sealed class OldPathHeader : PatchEntryAdditionalHeader
{
    internal OldPathHeader(Patch root,
                           PatchToken dashDashDashToken,
                           PatchToken<string> path)
        : base(root)
    {
        DashDashDashToken = dashDashDashToken;
        Path = path;
    }

    public override PatchNodeKind Kind => PatchNodeKind.OldPathHeader;

    public PatchToken DashDashDashToken { get; }

    public PatchToken<string> Path { get; }

    public override IEnumerable<PatchNode> Children() =>
    [
        DashDashDashToken,
        Path
    ];
}