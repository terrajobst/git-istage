namespace GitIStage.Patches;

public abstract class PatchHunkLine : PatchLine
{
    private protected PatchHunkLine(Patch root)
        : base(root)
    {
    }
}