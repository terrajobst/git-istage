namespace GitIStage.Services;

internal sealed class UserEnvironment
{
    public UserEnvironment(string? userProfile = null)
    {
        UserHomeDirectory = userProfile ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    public string UserHomeDirectory { get; }
}