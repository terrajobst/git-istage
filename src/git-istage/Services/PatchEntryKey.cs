using System.Runtime.InteropServices;

using GitIStage.Patches;

namespace GitIStage.Services;

internal readonly struct PatchEntryKey : IEquatable<PatchEntryKey>
{
    private readonly PatchEntry _entry;
    private readonly int _hashCode;

    public PatchEntryKey(PatchEntry entry)
    {
        var span = GetSpan(entry);
        var spanBytes = MemoryMarshal.AsBytes(span);
        var hashCode = new HashCode();
        hashCode.AddBytes(spanBytes);

        _entry = entry;
        _hashCode = hashCode.ToHashCode();
    }

    private static ReadOnlySpan<char> GetSpan(PatchEntry entry)
    {
        return entry.Root.Text.AsSpan(entry.Span);
    }

    public bool Equals(PatchEntryKey other)
    {
        if (_hashCode != other._hashCode)
            return false;

        var span = GetSpan(_entry);
        var otherSpan = GetSpan(other._entry);
        return span.SequenceEqual(otherSpan);
    }

    public override bool Equals(object? obj)
    {
        return obj is PatchEntryKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _hashCode;
    }

    public static bool operator ==(PatchEntryKey left, PatchEntryKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PatchEntryKey left, PatchEntryKey right)
    {
        return !left.Equals(right);
    }

    public static implicit operator PatchEntryKey(PatchEntry entry)
    {
        return new PatchEntryKey(entry);
    }

    public static implicit operator PatchEntry(PatchEntryKey key)
    {
        return key._entry;
    }

    public static HashSet<PatchEntryKey> GetSet(Patch patch)
    {
        var result = new HashSet<PatchEntryKey>();
        foreach (var entry in patch.Entries)
            result.Add(entry);
        return result;
    }
}
