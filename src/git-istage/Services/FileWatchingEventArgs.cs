using System.Collections.Immutable;

namespace GitIStage.Services;

internal sealed class FileWatchingEventArgs : EventArgs
{
    public FileWatchingEventArgs(IEnumerable<FileSystemEventArgs> events)
    {
        ThrowIfNull(events);

        Events = events.ToImmutableArray();
    }

    public ImmutableArray<FileSystemEventArgs> Events { get; }
}