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
        Change = PatchEntryChange.Modified;

        foreach (var additionalHeader in additionalHeaders)
        {
            switch (additionalHeader.Kind)
            {
                case PatchNodeKind.NewPathHeader:
                    var newPathHeader = (NewPathHeader)additionalHeader;
                    if (!string.IsNullOrEmpty(newPathHeader.Path.Value))
                        NewPath = newPathHeader.Path.Value;
                    break;
                case PatchNodeKind.OldPathHeader:
                    var oldPathHeader = (OldPathHeader)additionalHeader;
                    if (!string.IsNullOrEmpty(oldPathHeader.Path.Value))
                        OldPath = oldPathHeader.Path.Value;
                    break;
                case PatchNodeKind.IndexHeader:
                    var indexHeader = (IndexHeader)additionalHeader;
                    if (indexHeader.Mode is not null)
                        OldMode = NewMode = indexHeader.Mode.Value;
                    break;
                case PatchNodeKind.NewModeHeader:
                    var newModeHeader = (NewModeHeader)additionalHeader;
                    NewMode = newModeHeader.Mode.Value;
                    if (!Hunks.Any())
                        Change = PatchEntryChange.ModeChanged;
                    break;
                case PatchNodeKind.OldModeHeader:
                    var oldModeHeader = (OldModeHeader)additionalHeader;
                    OldMode = oldModeHeader.Mode.Value;
                    if (!Hunks.Any())
                        Change = PatchEntryChange.ModeChanged;
                    break;
                case PatchNodeKind.NewFileModeHeader:
                    var newFileModeHeader = (NewFileModeHeader)additionalHeader;
                    OldMode = PatchEntryMode.Nonexistent;
                    NewMode = newFileModeHeader.Mode.Value;
                    Change = PatchEntryChange.Added;
                    break;
                case PatchNodeKind.DeletedFileModeHeader:
                    var deletedFileModeHeader = (DeletedFileModeHeader)additionalHeader;
                    OldMode =  deletedFileModeHeader.Mode.Value;
                    NewMode = PatchEntryMode.Nonexistent;
                    Change = PatchEntryChange.Deleted;
                    break;
                case PatchNodeKind.BinaryFilesDifferHeader:
                    var binaryFilesDifferHeader = (BinaryFilesDifferHeader)additionalHeader;
                    NewPath = binaryFilesDifferHeader.NewPath.Value;
                    OldPath = binaryFilesDifferHeader.OldPath.Value;
                    break;
                case PatchNodeKind.CopyFromHeader:
                    var copyFromHeader = (CopyFromHeader)additionalHeader;
                    OldPath = copyFromHeader.Path.Value;
                    if (!Hunks.Any())
                        Change = PatchEntryChange.Copied;
                    break;
                case PatchNodeKind.CopyToHeader:
                    var copyToHeader = (CopyToHeader)additionalHeader;
                    NewPath = copyToHeader.Path.Value;
                    if (!Hunks.Any())
                        Change = PatchEntryChange.Copied;
                    break;
                case PatchNodeKind.RenameFromHeader:
                    var renameFromHeader = (RenameFromHeader)additionalHeader;
                    OldPath = renameFromHeader.Path.Value;
                    if (!Hunks.Any())
                        Change = PatchEntryChange.Renamed;
                    break;
                case PatchNodeKind.RenameToHeader:
                    var renameToHeader = (RenameToHeader)additionalHeader;
                    NewPath = renameToHeader.Path.Value;
                    if (!Hunks.Any())
                        Change = PatchEntryChange.Renamed;
                    break;
            }
        }
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

    public override string ToString()
    {
        return $"{NewPath} ({Change})";
    }
}