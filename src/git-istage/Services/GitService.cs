using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using LibGit2Sharp;

namespace GitIStage.Services;

internal sealed class GitService : IDisposable
{
    private readonly GitEnvironment _environment;
    private readonly OperationLogService _logService;
    private Repository _repository;
    private int _updateCounter;

    public GitService(GitEnvironment environment, OperationLogService logService)
    {
        _environment = environment;
        _logService = logService;
        InitializeRepository();
    }

    [MemberNotNull(nameof(_repository))]
    public void InitializeRepository()
    {
        // NOTE: We're re-creating the repo because when we shell out to Git the repository state will be stale.
        _repository?.Dispose();
        _repository = new Repository(_environment.RepositoryPath);

        // HACK: Allows us to use relative paths
        Environment.CurrentDirectory = _repository.Info.WorkingDirectory;
    }

    public void Dispose()
    {
        _repository.Dispose();
    }

    public Repository Repository => _repository;

    public void Execute(GitOperation operation, bool capture = true)
    {
        Execute([operation], capture);
    }

    public void Execute(IEnumerable<GitOperation> operations, bool capture = true)
    {
        var executedOperations = new List<GitOperation>();

        foreach (var operation in operations)
        {
            if (operation.TempFile is not null)
                File.WriteAllText(operation.TempFile.Path, operation.TempFile.Content());
            try
            {
                var result = ExecuteGit(operation.Command, capture);
                var executedOperation = operation.WithResult(result);
                executedOperations.Add(executedOperation);
            }
            finally
            {
                if (operation.TempFile is not null)
                    File.Delete(operation.TempFile.Path);
            }
        }

        _logService.Log(executedOperations);

        var affectedFiles = executedOperations
            .SelectMany(o => o.AffectedFiles)
            .ToHashSet(StringComparer.Ordinal);

        if (affectedFiles.Any())
        {
            if (affectedFiles.Contains("*"))
                RaiseFullReset();
            else
                RaiseFileChange(affectedFiles);
        }
    }

    private GitOperationResult ExecuteGit(string arguments, bool capture)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _environment.PathToGit,
            WorkingDirectory = _repository.Info.WorkingDirectory,
            Arguments = arguments,
            CreateNoWindow = capture,
            UseShellExecute = false,
            RedirectStandardError = capture,
            RedirectStandardOutput = capture
        };

        var output = new ConcurrentQueue<string>();

        static void AddOutput(string? line, ConcurrentQueue<string> receiver, bool isError)
        {
            if (line is not null)
            {
                var marker = isError ? "!" : ":";
                receiver.Enqueue($"{marker} {line}");
            }
        }

        using var process = new Process();
        process.StartInfo = startInfo;

        if (capture)
        {
            process.OutputDataReceived += (_, e) => AddOutput(e.Data, output, isError: false);
            process.ErrorDataReceived += (_, e) => AddOutput(e.Data, output, isError: true);
        }

        process.Start();

        if (capture)
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        process.WaitForExit();

        return new GitOperationResult(process.ExitCode, [..output]);
    }

    public void StashUntrackedKeepIndex()
    {
        var operation = GitOperation.Stash(untracked: true, keepIndex: true);
        Execute(operation);
    }

    public void Commit(bool amend)
    {
        var operation = GitOperation.Commit(verbose: true, amend);
        Execute(operation, capture: false);
    }

    public bool IsIgnoredOrOutsideWorkingDirectory(string path)
    {
        var workingDirectory = Repository.Info.WorkingDirectory;
        if (!path.StartsWith(workingDirectory, StringComparison.Ordinal))
            return true;

        var current = Path.GetRelativePath(workingDirectory, path);
        while (current is not null && current.Length > 0)
        {
            var ignored = Repository.Ignore.IsPathIgnored(current);
            if (ignored)
                return true;

            current = Path.GetDirectoryName(current);
        }

        return false;
    }

    public string GetPatch(bool fullFileDiff, int contextLines, bool stage, IEnumerable<string>? affectedPaths = null)
    {
        var tipTree = Repository.Head.Tip?.Tree;

        var compareOptions = new CompareOptions();
        compareOptions.ContextLines = fullFileDiff ? int.MaxValue : contextLines;

        return stage
            ? Repository.Diff.Compare<LibGit2Sharp.Patch>(tipTree, DiffTargets.Index, affectedPaths, null, compareOptions)
            : Repository.Diff.Compare<LibGit2Sharp.Patch>(affectedPaths, true, null, compareOptions);
    }

    private void RaiseFullReset()
    {
        Raise(new RepositoryChangedEventArgs());
    }

    private void RaiseFileChange(IEnumerable<string> paths)
    {
        Raise(new RepositoryChangedEventArgs(paths));
    }

    private void Raise(RepositoryChangedEventArgs e)
    {
        if (_updateCounter > 0)
            return;

        InitializeRepository();
        RepositoryChanged?.Invoke(this, e);
    }

    public IDisposable SuspendEvents()
    {
        _updateCounter++;
        return new SuspendedEvents(this);
    }

    private void RestoreEvents()
    {
        _updateCounter--;

        if (_updateCounter == 0)
            RaiseFullReset();
    }

    public event EventHandler<RepositoryChangedEventArgs>? RepositoryChanged;

    private sealed class SuspendedEvents : IDisposable
    {
        private readonly GitService _gitService;

        public SuspendedEvents(GitService gitService)
        {
            ThrowIfNull(gitService);

            _gitService = gitService;
        }

        public void Dispose()
        {
            _gitService.RestoreEvents();
        }
    }
}
