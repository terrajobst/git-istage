using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace GitIStage
{
    public partial class KeyBindings
    {
        public static KeyBindings Load()
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
                    var command = customKeyBinding.Key;
                    var key = customKeyBinding.Value.KeyBindings?.FirstOrDefault();
                    if (key != null)
                    {
                        var binding = keyBindings.Handlers.SingleOrDefault(h => h.Key.ToLowerInvariant() == command.ToLowerInvariant()).Value;
                        if (binding != null)
                        {
                            if (customKeyBinding.Value.Default?.Length == 0)
                                binding.Default.Clear();
                            binding.Default.Add(key);
                        }
                    }
                }
            }

            return keyBindings;
        }

        private static IDictionary<string, CustomKeyBinding> LoadCustomKeyBindings(string keyBindingsPath)
        {
            using (var stream = File.Open(keyBindingsPath, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream))
            {
                var content = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<Dictionary<string, CustomKeyBinding>>(content);
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
    }
}