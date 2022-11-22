namespace GitIStage.Patches;

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