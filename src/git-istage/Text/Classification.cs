using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace GitIStage.Text;

public sealed class Classification
{
    private static readonly ConcurrentDictionary<string, Classification> Interned = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, Classification>.AlternateLookup<ReadOnlySpan<char>> Lookup = Interned.GetAlternateLookup<ReadOnlySpan<char>>();

    private const int StackAllocThreshold = 256;

    private readonly ImmutableArray<string> _scopes;

    private Classification(ImmutableArray<string> scopes)
    {
        _scopes = scopes;
    }

    public ImmutableArray<string> Scopes => _scopes;

    public static Classification Create(params IList<string> scopes)
    {
        var keyLength = scopes.Count - 1; // separators
        for (var i = 0; i < scopes.Count; i++)
            keyLength += scopes[i].Length;

        Span<char> buffer = keyLength <= StackAllocThreshold
            ? stackalloc char[keyLength]
            : new char[keyLength];

        var offset = 0;
        for (var i = 0; i < scopes.Count; i++)
        {
            if (i > 0)
                buffer[offset++] = ' ';
            scopes[i].AsSpan().CopyTo(buffer[offset..]);
            offset += scopes[i].Length;
        }

        var key = buffer[..offset];

        if (Lookup.TryGetValue(key, out var existing))
            return existing;

        var keyString = key.ToString();
        var immutableScopes = scopes.ToImmutableArray();
        return Interned.GetOrAdd(keyString, static (_, s) => new Classification(s), immutableScopes);
    }
}
