namespace GitIStage.Tests.Infrastructure;

public abstract class KeyBindingServiceTestsBase : IDisposable
{
    private readonly KeyBindingService _keyBindingService;
    private readonly string _userProfile;

    protected KeyBindingServiceTestsBase()
    {
        _userProfile = TempDirectory.Create();
        var userEnvironment = new UserEnvironment(_userProfile);
        _keyBindingService = new KeyBindingService(userEnvironment);
    }

    public void Dispose()
    {
        Directory.Delete(_userProfile, recursive: true);
    }

    internal KeyBindingService KeyBindingService => _keyBindingService;

    internal void WriteSettings(string text)
    {
        var fileName = _keyBindingService.GetUserKeyBindingsPath();
        var directory = Path.GetDirectoryName(fileName)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(fileName, text);
    }
}