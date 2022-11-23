namespace GitIStage.Tests.Infrastructure;

internal static class TempDirectory
{
    public static string Create()
    {
        var path = Path.Join(Path.GetTempPath(), "git-istage-test_" + Guid.NewGuid());
        Directory.CreateDirectory(path);

        return path;
    }
}