using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using GitIStage.Patches;
using LibGit2Sharp;

using Patch = GitIStage.Patches.Patch;

namespace GitIStage.Services;

internal sealed class GitService : IDisposable
{
    private readonly GitEnvironment _environment;
    private Repository _repository;
    private int _updateCounter;

    public GitService(GitEnvironment environment)
    {
        _environment = environment;
        InitializeRepo();
    }

    [MemberNotNull(nameof(_repository))]
    private void InitializeRepo()
    {
        // NOTE: We're re-creating the repo because when we shell out to Git the repository state will be stale.
        _repository?.Dispose();
        _repository = new Repository(_environment.RepositoryPath);
    }

    public void Dispose()
    {
        _repository.Dispose();
    }

    public Repository Repository => _repository;

    public void ApplyPatch(Patch patch, PatchDirection direction)
    {
        var isUndo = direction is PatchDirection.Reset or PatchDirection.Unstage;
        var patchFilePath = Path.GetTempFileName();
        var reverse = isUndo ? "--reverse" : string.Empty;
        var cached = direction == PatchDirection.Reset ? string.Empty : "--cached";
        var command = $@"apply -v {cached} {reverse} --whitespace=nowarn ""{patchFilePath}""";

        File.WriteAllText(patchFilePath, patch.ToString());
        try
        {
            ExecuteGit(command);
        }
        catch (GitCommandFailedException ex)
        {
            var messageBuilder = new StringBuilder(ex.Message);
            messageBuilder.AppendLine();
            messageBuilder.AppendLine("Patch:");
            messageBuilder.Append(patch);
            throw new GitCommandFailedException(messageBuilder.ToString());
        }
        finally
        {
            File.Delete(patchFilePath);
        }

        var paths = patch.Entries.SelectMany(e => new[] { e.OldPath, e.NewPath })
            .Where(p => !string.IsNullOrEmpty(p))
            .ToArray();

        RaiseFileChange(paths);
    }

    private void ExecuteGit(string arguments, bool capture = true)
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

        var log = new List<string>();

        void Handler(object _, DataReceivedEventArgs e)
        {
            lock (log)
            {
                if (e.Data is not null)
                    log.Add(e.Data);
            }
        }

        using var process = new Process();
        process.StartInfo = startInfo;

        if (capture)
        {
            process.OutputDataReceived += Handler;
            process.ErrorDataReceived += Handler;
        }

        process.Start();

        if (capture)
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        process.WaitForExit();

        if (capture)
        {
            var logContainsErrors = log.Any(l => l.StartsWith("fatal:", StringComparison.Ordinal) ||
                                                 l.StartsWith("error:", StringComparison.Ordinal));

            if (logContainsErrors)
                throw ExceptionBuilder.GitCommandFailed(arguments, log);
        }
    }

    public void Reset(string path)
    {
        ExecuteGit($"reset \"{path}\"");
        RaiseFileChange(path);
    }

    public void Add(string path)
    {
        ExecuteGit($"add \"{path}\"");
        RaiseFileChange(path);
    }

    public void RemoveForce(string path)
    {
        ExecuteGit($"rm -f \"{path}\"");
        RaiseFileChange(path);
    }

    public void Checkout(string path)
    {
        ExecuteGit($"checkout \"{path}\"");
        RaiseFileChange(path);
    }

    public void RestoreStaged(string path)
    {
        ExecuteGit($"restore --staged \"{path}\"");
        RaiseFileChange(path);
    }

    public void StashUntrackedKeepIndex()
    {
        ExecuteGit("stash -u -k");
        RaiseFullReset();
    }

    public void Commit(bool amend)
    {
        var amendSwitch = amend ? "--amend " : string.Empty;
        ExecuteGit($"commit -v {amendSwitch}", capture: false);
        RaiseFullReset();
    }

    private void RaiseFullReset()
    {
        Raise(new RepositoryChangedEventArgs());
    }

    private void RaiseFileChange(string path)
    {
        Raise(new RepositoryChangedEventArgs(path));
    }

    private void RaiseFileChange(IEnumerable<string> paths)
    {
        Raise(new RepositoryChangedEventArgs(paths));
    }

    private void Raise(RepositoryChangedEventArgs e)
    {
        if (_updateCounter > 0)
            return;

        InitializeRepo();
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