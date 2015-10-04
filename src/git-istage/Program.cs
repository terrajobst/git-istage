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
            var searchPaths = paths.Select(p => Path.Combine(p, "git.exe"));
            return searchPaths.FirstOrDefault(File.Exists);
        }
    }
}