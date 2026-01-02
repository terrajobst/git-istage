namespace GitIStage.Patches;

internal readonly struct PatchTokenHandle
{
    private readonly PatchTokenizer _builder;
    private readonly int _tokenIndex;

    public PatchTokenHandle(PatchTokenizer builder, int tokenIndex)
    {
        _builder = builder;
        _tokenIndex = tokenIndex;
    }

    public static implicit operator PatchToken(PatchTokenHandle handle)
    {
        return handle._builder.CreateToken(handle._tokenIndex);
    }
}