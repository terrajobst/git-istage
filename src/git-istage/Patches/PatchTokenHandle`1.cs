namespace GitIStage.Patches;

internal readonly struct PatchTokenHandle<T>
{
    private readonly PatchTokenizer _builder;
    private readonly int _tokenIndex;

    public PatchTokenHandle(PatchTokenizer builder, int tokenIndex)
    {
        _builder = builder;
        _tokenIndex = tokenIndex; 
    }

    public static implicit operator PatchToken<T>(PatchTokenHandle<T> handle)
    {
        return handle._builder.CreateToken<T>(handle._tokenIndex);
    }
}
