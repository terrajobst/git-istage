using System;

namespace GitIStage
{
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
}