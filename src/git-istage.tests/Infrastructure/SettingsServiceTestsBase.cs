namespace GitIStage.Tests.Infrastructure;

public abstract class SettingsServiceTestsBase : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly string _userProfile;

    protected SettingsServiceTestsBase()
    {
        _userProfile = TempDirectory.Create();
        var userEnvironment = new UserEnvironment(_userProfile);
        _settingsService = new SettingsService(userEnvironment);
    }

    public void Dispose()
    {
        Directory.Delete(_userProfile, recursive: true);
    }

    internal SettingsService SettingsService => _settingsService;

    internal void WriteSettings(string text)
    {
        var fileName = _settingsService.GetUserKeyBindingsPath();
        var directory = Path.GetDirectoryName(fileName)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(fileName, text);
    }
}
