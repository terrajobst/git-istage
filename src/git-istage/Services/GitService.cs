using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using GitIStage.Patches;
using LibGit2Sharp;

namespace GitIStage.Services;

internal sealed class GitService
{
    private readonly GitEnvironment _environment;
    private Repository _repository;

    public GitService(GitEnvironment environment)
    {
        _environment = environment;
        UpdateRepository();
    }

    public Repository Repository => _repository;

    [MemberNotNull(nameof(_repository))]
    private void UpdateRepository()
    {
        // NOTE: Why are we doing this again? Seems not necessary.
        _repository?.Dispose();
        _repository = new Repository(_environment.RepositoryPath);

        RepositoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyPatch(string patch, PatchDirection direction)
    {
        Patching.ApplyPatch(_environment.PathToGit, _repository.Info.WorkingDirectory, patch, direction);
        UpdateRepository();
    }

    public void RunCommand(string command)
    {
        var startupInfo = new ProcessStartInfo
        {
            FileName = _environment.PathToGit,
            Arguments = command,
            CreateNoWindow = true,
            UseShellExecute = false,
            WorkingDirectory = _repository.Info.WorkingDirectory
        };

        using var process = Process.Start(startupInfo);
        process?.WaitForExit();

        UpdateRepository();
    }

    public event EventHandler? RepositoryChanged;
}