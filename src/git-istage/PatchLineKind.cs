using System;

namespace GitIStage
{
    internal enum PatchLineKind
    {
        Header,
        Hunk,
        Context,
        Addition,
        Removal,
        NoEndOfLine
    }
}