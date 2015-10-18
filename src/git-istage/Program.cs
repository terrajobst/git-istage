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
            var repositoryPath = ResolveRepositoryPath();
            if (!Repository.IsValid(repositoryPath))
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

            var application = new Application(repositoryPath, pathToGit);
            application.Run();
        }

        private static string ResolveRepositoryPath()
        {
            return Directory.GetCurrentDirectory();
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