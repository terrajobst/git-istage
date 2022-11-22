namespace GitIStage.Services;

internal sealed class GitEnvironment
{
    public GitEnvironment(string repositoryPath, string pathToGit)
    {
        RepositoryPath = repositoryPath;
        PathToGit = pathToGit;
    }

    public string RepositoryPath { get; }

    public string PathToGit { get; }
}