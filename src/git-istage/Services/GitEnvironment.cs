using LibGit2Sharp;

namespace GitIStage.Services;

internal sealed class GitEnvironment
{
    public GitEnvironment()
        : this(null, null)
    {
    }

    public GitEnvironment(string? repositoryPath = null, string? pathToGit = null)
    {
        RepositoryPath = repositoryPath ?? ResolveRepositoryPath();
        PathToGit = pathToGit ?? ResolveGitPath();

        if (string.IsNullOrEmpty(RepositoryPath))
            throw ExceptionBuilder.NotAGitRepository();

        if (string.IsNullOrEmpty(PathToGit))
            throw ExceptionBuilder.GitNotFound();
    }

    public string RepositoryPath { get; }

    public string PathToGit { get; }

    private static string ResolveRepositoryPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var repositoryPath = Repository.Discover(currentDirectory);

        if (string.IsNullOrEmpty(repositoryPath))
            return string.Empty;

        if (!Repository.IsValid(repositoryPath))
            return string.Empty;

        return repositoryPath;
    }

    private static string ResolveGitPath()
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        var paths = path.Split(Path.PathSeparator);

        // In order to have this work across all operating systems, we
        // need to include other extensions.
        //
        // NOTE: On .NET Core, we should use RuntimeInformation in order
        //       to limit the extensions based on operating system.

        var fileNames = new[] { "git.exe", "git" };
        var searchPaths = paths.SelectMany(p => fileNames.Select(f => Path.Combine(p, f)));

        return searchPaths.FirstOrDefault(File.Exists) ?? string.Empty;
    }
}