namespace GitIStage.Services;

internal sealed class TempFile
{
    public static TempFile CreateWithExtension(string extension, Func<string> content)
    {
        var tempDirectory = System.IO.Path.GetTempPath();
        var tempFileName = System.IO.Path.Join(tempDirectory, $"{Guid.NewGuid():N}{extension}");
        return new TempFile(tempFileName, content);
    }

    private TempFile(string path, Func<string> content)
    {
        Path = path;
        Content = content;
    }

    public string Path { get; }

    public Func<string> Content { get; }
}