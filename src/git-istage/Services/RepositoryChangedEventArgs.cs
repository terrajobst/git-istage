using System.Collections.Immutable;

namespace GitIStage.Services;

public sealed class RepositoryChangedEventArgs : EventArgs
{
    public RepositoryChangedEventArgs()
    {
        AffectedPaths = [];
    }

    public RepositoryChangedEventArgs(IEnumerable<string> affectedPaths)
    {
        ThrowIfNull(affectedPaths);

        AffectedPaths = [..affectedPaths];
    }

    public ImmutableArray<string> AffectedPaths { get; }
}