namespace GitIStage.Patches.EntryHeaders;

public sealed class NewModeHeader : PatchEntryAdditionalHeader
{
    internal NewModeHeader(Patch root,
                           PatchToken newKeyword,
                           PatchToken modeKeyword,
                           PatchToken<PatchEntryMode> mode)
        : base(root)
    {
        NewKeyword = newKeyword;
        ModeKeyword = modeKeyword;
        Mode = mode;
    }

    public override PatchNodeKind Kind => PatchNodeKind.NewModeHeader;

    public PatchToken NewKeyword { get; }

    public PatchToken ModeKeyword { get; }

    public PatchToken<PatchEntryMode> Mode { get; }

    public override IEnumerable<PatchNode> Children() =>
    [
        NewKeyword,
        ModeKeyword,
        Mode
    ];
}