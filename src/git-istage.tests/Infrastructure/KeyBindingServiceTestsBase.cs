namespace GitIStage.Tests.Infrastructure;

public abstract class KeyBindingServiceTestsBase
{
    private readonly KeyBindingService _keyBindingService;

    protected KeyBindingServiceTestsBase()
    {
        var userProfile = TempDirectory.Create();
        var userEnvironment = new UserEnvironment(userProfile);
        _keyBindingService = new KeyBindingService(userEnvironment);
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