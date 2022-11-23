using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using GitIStage.Patches;
using LibGit2Sharp;

namespace GitIStage.Services;

internal sealed class GitService : IDisposable
{
    private readonly GitEnvironment _environment;
    private Repository _repository;

    public GitService(GitEnvironment environment)
    {
        _environment = environment;
        UpdateRepository();
    }

    public void Dispose()
    {
        _repository.Dispose();
    }

    public Repository Repository => _repository;

    [MemberNotNull(nameof(_repository))]
    private void UpdateRepository()
    {
        // NOTE: We're re-creating the repo because when we shell out to Git the repository state will be stale.
        _repository?.Dispose();
        _repository = new Repository(_environment.RepositoryPath);

        RepositoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyPatch(string patch, PatchDirection direction)
    {
        var isUndo = direction is PatchDirection.Reset or PatchDirection.Unstage;
        var patchFilePath = Path.GetTempFileName();
        var reverse = isUndo ? "--reverse" : string.Empty;
        var cached = direction == PatchDirection.Reset ? string.Empty : "--cached";
        var command = $@"apply -v {cached} {reverse} --whitespace=nowarn ""{patchFilePath}""";

        File.WriteAllText(patchFilePath, patch);
        try
        {
            ExecuteGit(command);
        }
        finally
        {
            File.Delete(patchFilePath);
        }

        UpdateRepository();
    }

    public void ExecuteGit(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _environment.PathToGit,
            WorkingDirectory = _repository.Info.WorkingDirectory,
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
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
        process.OutputDataReceived += Handler;
        process.ErrorDataReceived += Handler;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        var logContainsErrors = log.Any(l => l.StartsWith("fatal:", StringComparison.Ordinal) ||
                                             l.StartsWith("error:", StringComparison.Ordinal));

        if (logContainsErrors)
            throw ExceptionBuilder.GitCommandFailed(arguments, log);

        UpdateRepository();
    }

    public event EventHandler? RepositoryChanged;
}