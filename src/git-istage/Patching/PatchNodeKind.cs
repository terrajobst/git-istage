namespace GitIStage.Patching;

public enum PatchNodeKind
{
    // Nodes
    Patch,
    Entry,
    Hunk,

    // Entry Lines
    DiffGitHeader,
    OldPathHeader,
    NewPathHeader,
    OldModeHeader,
    NewModeHeader,
    DeletedFileModeHeader,
    NewFileModeHeader,
    CopyFromHeader,
    CopyToHeader,
    RenameFromHeader,
    RenameToHeader,
    SimilarityIndexHeader,
    DissimilarityIndexHeader,
    IndexHeader,
    UnknownHeader,

    // Hunk Lines
    HunkHeader,
    ContextLine,
    AddedLine,
    DeletedLine,
    NoNewLine
}
