using System.Collections.Concurrent;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;

namespace GitIStage.Services;

internal sealed class FileWatchingService : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentQueue<FileSystemEventArgs> _events = new();
    private readonly FileSystemWatcher _watcher;
    private readonly Timer _notificationTimer;
    private int _updateCounter;

    public FileWatchingService(IServiceProvider serviceProvider, GitEnvironment gitEnvironment)
    {
        _serviceProvider = serviceProvider;

        string workingDirectory;
        using (var r = new Repository(gitEnvironment.RepositoryPath))
            workingDirectory = r.Info.WorkingDirectory;

        _watcher = new FileSystemWatcher(workingDirectory);
        _watcher.NotifyFilter = NotifyFilters.Attributes
                                | NotifyFilters.CreationTime
                                | NotifyFilters.DirectoryName
                                | NotifyFilters.FileName
                                | NotifyFilters.LastAccess
                                | NotifyFilters.LastWrite
                                | NotifyFilters.Security
                                | NotifyFilters.Size;

        _watcher.Changed += OnChanged;
        _watcher.Created += OnCreated;
        _watcher.Deleted += OnDeleted;
        _watcher.Renamed += OnRenamed;
        _watcher.IncludeSubdirectories = true;
        _watcher.EnableRaisingEvents = true;
        _notificationTimer = new Timer(_ => Update());
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }

    public IDisposable SuspendEvents()
    {
        _updateCounter++;
        _watcher.EnableRaisingEvents = false;
        return new SuspendedEvents(this);
    }

    private void RestoreEvents()
    {
        _updateCounter--;

        if (_updateCounter == 0)
            _watcher.EnableRaisingEvents = true;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        AddFile(e);
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        AddFile(e);
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        AddFile(e);
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        AddFile(e);
    }

    private void AddFile(FileSystemEventArgs e)
    {
        _events.Enqueue(e);
        _notificationTimer.Change(TimeSpan.FromMilliseconds(500), Timeout.InfiniteTimeSpan);
    }

    private void Update()
    {

        var events = new List<FileSystemEventArgs>();
        while (_events.TryDequeue(out var e))
            events.Add(e);

        var application = _serviceProvider.GetRequiredService<Application>();
        application.Invoke(() =>
        {
            Changed?.Invoke(this, new FileWatchingEventArgs(events));
        });
    }

    public event EventHandler<FileWatchingEventArgs>? Changed;

    private sealed class SuspendedEvents : IDisposable
    {
        private readonly FileWatchingService _service;

        public SuspendedEvents(FileWatchingService service)
        {
            ThrowIfNull(service);

            _service = service;
        }

        public void Dispose()
        {
            _service.RestoreEvents();
        }
    }
}