namespace GitIStage.Services;

internal sealed class UserEnvironment
{
    public UserEnvironment(string? settingsDirectory = null)
    {
        if (settingsDirectory is null)
        {
            var configDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            settingsDirectory = Path.Join(configDirectory, ".git-istage");

            // Migrate previous location
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var oldSettingsDirectory = Path.Join(userProfile, ".git-istage");

            if (Directory.Exists(oldSettingsDirectory) && !Directory.Exists(settingsDirectory))
            {
                var parentDirectory = Path.GetDirectoryName(settingsDirectory);
                if (parentDirectory is not null)
                    Directory.CreateDirectory(parentDirectory);

                Directory.Move(oldSettingsDirectory, settingsDirectory);

                Console.WriteLine($"info: settings were migrated from '{oldSettingsDirectory}' to '{settingsDirectory}'");
            }
        }

        SettingsDirectory = settingsDirectory;
    }

    public string SettingsDirectory { get; }
}