namespace GitIStage.Tests.Infrastructure;

public sealed class RepositoryTempDirectory : IDisposable
{
    private readonly string _filePath;
    private readonly string _directoryPath;

    public RepositoryTempDirectory(string filePath, string directoryPath)
    {
        _filePath = filePath;
        _directoryPath = directoryPath;
    }

    public void Dispose()
    {
        try
        {
            // Directory.Delete() will fail for any read only files. Need to reset attributes first.
            foreach (var file in Directory.EnumerateFiles(_directoryPath, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);

            Directory.Delete(_directoryPath, recursive: true);
        }
        catch (Exception)
        {
            // Ignore exceptions -- the directory will be cleaned up by the OS eventually.
        }

        try
        {
            File.Delete(_filePath);
        }
        catch (Exception)
        {
            // Ignore exceptions -- the file will be cleaned up by the OS eventually.
        }
    }

    public static RepositoryTempDirectory Create()
    {
        var filePath = Path.GetTempFileName();
        var name = Path.GetFileName(filePath);
        var directoryPath = Path.Join(Path.GetTempPath(), $"git-istage-test_{name}");
        return new RepositoryTempDirectory(filePath, directoryPath);
    }

    public static implicit operator string(RepositoryTempDirectory name) => name._directoryPath;
}
