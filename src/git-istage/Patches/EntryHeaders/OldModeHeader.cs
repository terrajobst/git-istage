namespace GitIStage.Patches.EntryHeaders;

public sealed class OldModeHeader : PatchEntryAdditionalHeader
{
    internal OldModeHeader(Patch root,
                           PatchToken oldKeyword,
                           PatchToken modeKeyword,
                           PatchToken<PatchEntryMode> mode)
        : base(root)
    {
        OldKeyword = oldKeyword;
        ModeKeyword = modeKeyword;
        Mode = mode;
    }

    public override PatchNodeKind Kind => PatchNodeKind.OldModeHeader;

    public PatchToken OldKeyword { get; }

    public PatchToken ModeKeyword { get; }

    public PatchToken<PatchEntryMode> Mode { get; }

    public override IEnumerable<PatchNode> Children() =>
    [
        OldKeyword,
        ModeKeyword,
        Mode
    ];
}