using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using LibGit2Sharp;
using Newtonsoft.Json;

namespace GitIStage
{
    internal static class Program
    {
        private static void Main()
        {
            if (Win32Console.IsSupported)
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

            var keyBindings = LoadKeyBindings();

            var application = new Application(repositoryPath, pathToGit, keyBindings);
            application.Run();
        }

        private static KeyBindings LoadKeyBindings()
        {
            // load default key bindings from resource

            KeyBindings keyBindings = null;

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("git-istage.key-bindings.json"))
                keyBindings = LoadKeyBindings(stream);

            // merge custom key bindings

            var keyBindingsPath = ResolveCustomKeyBindingsPath();
            if (!string.IsNullOrEmpty(keyBindingsPath) && File.Exists(keyBindingsPath))
            {
                var customKeyBindings = LoadCustomKeyBindings(keyBindingsPath);
                foreach (var customKeyBinding in customKeyBindings)
                {
                    var binding = keyBindings.Handlers.Values.SingleOrDefault(v => v.Default.Any(k => k.ToLowerInvariant() == customKeyBinding.Before.ToLowerInvariant()));
                    binding.Default = ReplaceEntry(binding.Default, customKeyBinding.Before, customKeyBinding.After);
                }
            }

            return keyBindings;
        }

        private static CustomKeyBinding[] LoadCustomKeyBindings(string keyBindingsPath)
        {
            using (var stream = File.Open(keyBindingsPath, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream))
            {
                var content = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<CustomKeyBinding[]>(content);
            }
        }

        private static KeyBindings LoadKeyBindings(Stream stream)
        {
            var keyBindings = new KeyBindings();

            using (var reader = new StreamReader(stream))
            {
                var content = reader.ReadToEnd();
                keyBindings.Handlers = JsonConvert.DeserializeObject<Dictionary<string, KeyBinding>>(content);
            }

            return keyBindings;
        }

        private static string ResolveCustomKeyBindingsPath()
        {
            const string keyBindingsJsonFileName = ".git-istage/key-bindings.json";
            var appDirectory = GetHomeDirectory();
            return Path.Combine(appDirectory, keyBindingsJsonFileName);
        }

        private static string GetHomeDirectory()
        {
            string homePath =
                (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    ? Environment.GetEnvironmentVariable("HOME")
                    : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%")
                    ;

            return homePath;
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

        private static string[] ReplaceEntry(string[] array, string entry, string replacedBy)
        {
            var entries = new string[array.Length];

            for (var index = 0; index < array.Length; index++)
            {
                entries[index] = string.Compare(array[index], entry, true) == 0
                    ? replacedBy
                    : array[index]
                    ;
            }

            return entries;
        }
    }
}