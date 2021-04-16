using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace GitIStage
{
    internal static class Program
    {
        private static void Main()
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

            var keyBindings = KeyBindings.Load();

            var application = new Application(repositoryPath, pathToGit, keyBindings);
            application.Run();
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
    }
}