namespace GitIStage.Patches;

public enum PatchNodeKind
{
    // Trivia
    SpaceTrivia,
    EndOfLineTrivia,

    // Tokens
    PathToken,
    HashToken,
    ModeToken,
    TextToken,
    PercentageToken,
    RangeToken,

    // Operators
    MinusMinusToken,
    MinusMinusMinusToken,
    PlusPlusPlusToken,
    DotDotToken,
    AtAtToken,
    MinusToken,
    PlusToken,
    SpaceToken,
    BackslashToken,

    // Keywords
    DiffKeyword,
    GitKeyword,
    IndexKeyword,
    NewKeyword,
    FileKeyword,
    ModeKeyword,
    DeletedKeyword,
    OldKeyword,
    CopyKeyword,
    FromKeyword,
    ToKeyword,
    RenameKeyword,
    SimilarityKeyword,
    DissimilarityKeyword,

    // Nodes
    Patch,
    Entry,
    EntryHeader,
    Hunk,
    HunkHeader,

    // Entry Headers
    IndexHeader,
    NewPathHeader,
    OldPathHeader,
    NewFileModeHeader,
    DeletedFileModeHeader,
    NewModeHeader,
    OldModeHeader,
    CopyFromHeader,
    CopyToHeader,
    RenameFromHeader,
    RenameToHeader,
    SimilarityIndexHeader,
    DissimilarityIndexHeader,

    // Hunk Lines
    ContextLine,
    AddedLine,
    DeletedLine,
    NoFinalLineBreakLine,
}