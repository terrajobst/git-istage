using GitIStage.UI;

namespace GitIStage.Tests;

public class FileLevelTests : RepositoryTests
{
    [Fact]
    public void FileLevel_GetDocument()
    {
        TouchFile("unchanged.txt");
        TouchFile("modified.txt");
        TouchFile("deleted.txt");
        StageAll();
        Commit();

        TouchFile("added.txt");
        WriteFile("modified.txt", "changes");
        DeleteFile("deleted.txt");

        var expectedChanges = """
            + added.txt
            ~ modified.txt
            - deleted.txt
            """;

        ViewFiles = true;
        var unstaged = GetDocument<FileDocument>();
        AssertFiles(expectedChanges, unstaged);

        ViewStage = true;
        var staged = GetDocument<FileDocument>();
        AssertFilesEmpty(staged);
    }

    [Fact]
    public void FileLevel_Stage_Added()
    {
        TouchFile("added.txt");

        var line = "*added.txt";

        var expectedStaged = """
            + added.txt
            """;

        ViewFiles = true;
        StageLine(line);

        var unstaged = GetDocument<FileDocument>();
        AssertFilesEmpty(unstaged);

        ViewStage = true;
        var staged = GetDocument<FileDocument>();
        AssertFiles(expectedStaged, staged);
    }

    [Fact]
    public void FileLevel_Stage_Modified()
    {
        TouchFile("modified.txt");
        StageAll();
        Commit();

        WriteFile("modified.txt", "change");

        var line = "*modified.txt";

        var expectedStaged = """
            ~ modified.txt
            """;

        ViewFiles = true;
        StageLine(line);

        var unstaged = GetDocument<FileDocument>();
        AssertFilesEmpty(unstaged);

        ViewStage = true;
        var staged = GetDocument<FileDocument>();
        AssertFiles(expectedStaged, staged);
    }

    [Fact]
    public void FileLevel_Stage_Deleted()
    {
        TouchFile("deleted.txt");
        StageAll();
        Commit();

        DeleteFile("deleted.txt");

        var line = "*deleted.txt";

        var expectedStaged = """
            - deleted.txt
            """;

        ViewFiles = true;
        StageLine(line);

        var unstaged = GetDocument<FileDocument>();
        AssertFilesEmpty(unstaged);

        ViewStage = true;
        var staged = GetDocument<FileDocument>();
        AssertFiles(expectedStaged, staged);
    }

    [Fact]
    public void FileLevel_StageHunk_BehavesLike_StageLine()
    {
        TouchFile("unchanged.txt");
        TouchFile("modified.txt");
        TouchFile("deleted.txt");
        StageAll();
        Commit();

        TouchFile("added.txt");
        WriteFile("modified.txt", "changes");
        DeleteFile("deleted.txt");

        var line = "*modified.txt";

        var expectedUnstaged = """
            + added.txt
            - deleted.txt
            """;

        var expectedStaged = """
            ~ modified.txt
            """;

        ViewFiles = true;
        StageHunk(line);

        var unstaged = GetDocument<FileDocument>();
        AssertFiles(expectedUnstaged, unstaged);

        ViewStage = true;
        var staged = GetDocument<FileDocument>();
        AssertFiles(expectedStaged, staged);
    }

    [Fact]
    public void FileLevel_Unstage_Added()
    {
        TouchFile("added.txt");
        StageAll();

        var line = "*added.txt";

        var expectedUnstaged = """
            + added.txt
            """;

        ViewFiles = true;
        ViewStage = true;
        UnstageLine(line);

        var staged = GetDocument<FileDocument>();
        AssertFilesEmpty(staged);

        ViewStage = false;
        var unstaged = GetDocument<FileDocument>();
        AssertFiles(expectedUnstaged, unstaged);
    }

    [Fact]
    public void FileLevel_Unstage_Modified()
    {
        TouchFile("modified.txt");
        StageAll();
        Commit();

        WriteFile("modified.txt", "modified");
        StageAll();

        var line = "*modified.txt";

        var expectedUnstaged = """
            ~ modified.txt
            """;

        ViewFiles = true;
        ViewStage = true;
        UnstageLine(line);

        var staged = GetDocument<FileDocument>();
        AssertFilesEmpty(staged);

        ViewStage = false;
        var unstaged = GetDocument<FileDocument>();
        AssertFiles(expectedUnstaged, unstaged);
    }

    [Fact]
    public void FileLevel_Unstage_Deleted()
    {
        TouchFile("deleted.txt");
        StageAll();
        Commit();

        DeleteFile("deleted.txt");
        StageAll();

        var line = "*deleted.txt";

        var expectedUnstaged = """
            - deleted.txt
            """;

        ViewFiles = true;
        ViewStage = true;
        UnstageLine(line);

        var staged = GetDocument<FileDocument>();
        AssertFilesEmpty(staged);

        ViewStage = false;
        var unstaged = GetDocument<FileDocument>();
        AssertFiles(expectedUnstaged, unstaged);
    }

    [Fact]
    public void FileLevel_UnstageHunk_BehavesLike_UnstageLine()
    {
        TouchFile("file1.txt");
        TouchFile("file2.txt");
        TouchFile("file3.txt");
        StageAll();

        var line = "*file2.txt";

        var expectedStaged = """
            + file1.txt
            + file3.txt
            """;

        var expectedUnstaged = """
            + file2.txt
            """;

        ViewFiles = true;
        ViewStage = true;
        UnstageHunk(line);

        var staged = GetDocument<FileDocument>();
        AssertFiles(expectedStaged, staged);

        ViewStage = false;
        var unstaged = GetDocument<FileDocument>();
        AssertFiles(expectedUnstaged, unstaged);
    }

    [Fact]
    public void FileLevel_Reset_Added()
    {
        TouchFile("added.txt");

        var line = "*added.txt";

        ViewFiles = true;
        ResetLine(line);

        var staged = GetDocument<FileDocument>();
        AssertFilesEmpty(staged);

        ViewStage = true;
        var unstaged = GetDocument<FileDocument>();
        AssertFilesEmpty(unstaged);
    }

    [Fact]
    public void FileLevel_Reset_Modified()
    {
        TouchFile("modified.txt");
        StageAll();
        Commit();

        WriteFile("modified.txt", "modified");

        var line = "*modified.txt";

        ViewFiles = true;
        ResetLine(line);

        var staged = GetDocument<FileDocument>();
        AssertFilesEmpty(staged);

        ViewStage = true;
        var unstaged = GetDocument<FileDocument>();
        AssertFilesEmpty(unstaged);
    }

    [Fact]
    public void FileLevel_Reset_Deleted()
    {
        TouchFile("deleted.txt");
        StageAll();
        Commit();

        DeleteFile("deleted.txt");

        var line = "*deleted.txt";

        ViewFiles = true;
        ResetLine(line);

        var staged = GetDocument<FileDocument>();
        AssertFilesEmpty(staged);

        ViewStage = true;
        var unstaged = GetDocument<FileDocument>();
        AssertFilesEmpty(unstaged);
    }

    [Fact]
    public void FileLevel_ResetHunk_BehavesLike_ResetLine()
    {
        TouchFile("file1.txt");
        TouchFile("file2.txt");
        TouchFile("file3.txt");
        StageAll();
        Commit();

        WriteFile("file1.txt", "change");
        WriteFile("file2.txt", "change");
        WriteFile("file3.txt", "change");

        var line = "*file2.txt";

        var expectedUnstaged = """
            ~ file1.txt
            ~ file3.txt
            """;

        ViewFiles = true;
        ResetHunk(line);

        var unstaged = GetDocument<FileDocument>();
        AssertFiles(expectedUnstaged, unstaged);

        ViewStage = true;
        var staged = GetDocument<FileDocument>();
        AssertFilesEmpty(staged);
    }
}