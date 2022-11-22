using GitIStage.Services;
using GitIStage.UI;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GitIStage;

internal static class Program
{
    private static async Task Main()
    {
        if (OperatingSystem.IsWindows())
            Win32Console.Initialize();

        var repositoryPath = ResolveRepositoryPath();
        if (string.IsNullOrEmpty(repositoryPath))
        {
            Console.WriteLine("fatal: Not a git repository");
            return;
        }

        var pathToGit = ResolveGitPath();
        if (string.IsNullOrEmpty(pathToGit))
        {
            Console.WriteLine("fatal: git is not in your path");
            return;
        }

        var gitEnvironment = new GitEnvironment(repositoryPath, pathToGit);

        using var host = Host.CreateDefaultBuilder()
                             .ConfigureServices(s => ConfigureServices(s, gitEnvironment))
                             .ConfigureLogging(l => l.ClearProviders())
                             .Build();
        host.Start();

        var application = host.Services.GetRequiredService<Application>();
        application.Run();

        await host.StopAsync();
        await host.WaitForShutdownAsync();
    }

    private static string ResolveRepositoryPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var repositoryPath = Repository.Discover(currentDirectory);

        if (string.IsNullOrEmpty(repositoryPath))
            return null;

        if (!Repository.IsValid(repositoryPath))
            return null;

        return repositoryPath;
    }

    private static string ResolveGitPath()
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
            return null;

        var paths = path.Split(Path.PathSeparator);

        // In order to have this work across all operating systems, we
        // need to include other extensions.
        //
        // NOTE: On .NET Core, we should use RuntimeInformation in order
        //       to limit the extensions based on operating system.

        var fileNames = new[] { "git.exe", "git" };
        var searchPaths = paths.SelectMany(p => fileNames.Select(f => Path.Combine(p, f)));

        return searchPaths.FirstOrDefault(File.Exists);
    }

    private static void ConfigureServices(IServiceCollection serviceCollection, GitEnvironment gitEnvironment)
    {
        serviceCollection.AddSingleton(gitEnvironment);
        serviceCollection.AddSingleton<Application>();
        serviceCollection.AddSingleton<GitService>();
        serviceCollection.AddSingleton<DocumentService>();
        serviceCollection.AddSingleton<UIService>();
        serviceCollection.AddSingleton<CommandService>();
        serviceCollection.AddSingleton<KeyBindingService>();
    }
}