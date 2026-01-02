using System.Collections.Immutable;
using GitIStage.Patches.EntryHeaders;

namespace GitIStage.Patches;

public sealed class PatchEntry : PatchNode
{
    internal PatchEntry(Patch root,
                        PatchEntryHeader header,
                        ImmutableArray<PatchEntryAdditionalHeader> additionalHeaders,
                        ImmutableArray<PatchHunk> hunks)
    {
        ThrowIfNull(root);
        ThrowIfNull(header);

        Root = root;
        Header = header;
        AdditionalHeaders = additionalHeaders;
        Hunks = hunks;
        OldPath = header.OldPath.Value;
        NewPath = header.NewPath.Value;

        foreach (var additionalHeader in additionalHeaders)
        {
            switch (additionalHeader.Kind)
            {
                case PatchNodeKind.NewPathHeader:
                    var nph = (NewPathHeader)additionalHeader;
                    NewPath = nph.Path.Value;
                    break;
                case PatchNodeKind.OldPathHeader:
                    var oph = (OldPathHeader)additionalHeader;
                    OldPath = oph.Path.Value;
                    break;
                case PatchNodeKind.IndexHeader:
                    var ih = (IndexHeader)additionalHeader;
                    if (ih.Mode is not null)
                        OldMode = NewMode = ih.Mode.Value;
                    break;
                case PatchNodeKind.NewModeHeader:
                    var nmh = (NewModeHeader)additionalHeader;
                    NewMode = nmh.Mode.Value;
                    Change = PatchEntryChange.ModeChanged;
                    break;
                case PatchNodeKind.OldModeHeader:
                    var omh = (OldModeHeader)additionalHeader;
                    OldMode = omh.Mode.Value;
                    Change = PatchEntryChange.ModeChanged;
                    break;
                case PatchNodeKind.NewFileModeHeader:
                    var nfmh = (NewFileModeHeader)additionalHeader;
                    OldMode = PatchEntryMode.Nonexistent;
                    NewMode = nfmh.Mode.Value;
                    Change = PatchEntryChange.Added;
                    break;
                case PatchNodeKind.DeletedFileModeHeader:
                    var dfmh = (DeletedFileModeHeader)additionalHeader;
                    OldMode =  dfmh.Mode.Value;
                    NewMode = PatchEntryMode.Nonexistent;
                    Change = PatchEntryChange.Deleted;
                    break;
                case PatchNodeKind.CopyFromHeader:
                case PatchNodeKind.CopyToHeader:
                    Change = PatchEntryChange.Copied;
                    break;
                case PatchNodeKind.RenameFromHeader:
                case PatchNodeKind.RenameToHeader:
                    Change = PatchEntryChange.Renamed;
                    break;
            }
        }

        if (Hunks.Any())
            Change = PatchEntryChange.Modified;
    }

    public override PatchNodeKind Kind => PatchNodeKind.Entry;

    public override Patch Root { get; }

    public PatchEntryHeader Header { get; }

    public ImmutableArray<PatchEntryAdditionalHeader> AdditionalHeaders { get; }

    public ImmutableArray<PatchHunk> Hunks { get; }

    public PatchEntryChange Change { get; }

    public string OldPath { get; }

    public PatchEntryMode OldMode { get; }

    public string NewPath { get; }

    public PatchEntryMode NewMode { get; }
    
    public override IEnumerable<PatchNode> Children() => [Header, ..AdditionalHeaders, ..Hunks];
}