namespace GitIStage.Patches;

// TODO: Delete this in favor of the new patches
internal enum PatchLineKind
{
    DiffLine,
    Header,
    Hunk,
    Context,
    Addition,
    Removal,
    NoEndOfLine
}