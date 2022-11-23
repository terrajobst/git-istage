namespace GitIStage.Tests;

// TODO: Test staging/unstaging/resetting files

public class PatchingServiceTests : RepositoryTests
{
    [Fact]
    public void Patching_GetPatch()
    {
        var original = """
            line 1
            line 2
            line 3
            """;
        
        var changed = """
            line 1
            line 3
            """;

        var expectedPatch = """
            -line 2
            """;

        WriteTheFile(original);
        StageTheFile();
        Commit();
        WriteTheFile(changed);

        var unstaged = GetDocument<PatchDocument>();
        AssertPatch(expectedPatch, unstaged);
        
        ViewStage = true;
        var staged = GetDocument<PatchDocument>();
        AssertPatchEmpty(staged);
    }
    
    [Fact]
    public void Patching_Stage_Line()
    {
        var original = """
            line 1
            line 2
            line 3
            """;
        
        var changed = """
            line 1
            line 3
            """;

        var line = "-line 2";

        var expectedPatch = """
            -line 2
            """;
        
        WriteTheFile(original);
        StageTheFile();
        Commit();
        WriteTheFile(changed);
        
        StageLine(line);

        var unstaged = GetDocument<PatchDocument>();
        AssertPatchEmpty(unstaged);
        
        ViewStage = true;
        var staged = GetDocument<PatchDocument>();
        AssertPatch(expectedPatch, staged);
    }
    
    [Fact]
    public void Patching_Stage_Hunk()
    {
        var original = """
            line 1
            line 2
            line 3
            line 4
            """;
        
        var changed = """
            line 1
            line 4
            """;

        var line = "-line 2";

        var expectedPatch = """
            -line 2
            -line 3
            """;
        
        WriteTheFile(original);
        StageTheFile();
        Commit();
        WriteTheFile(changed);
        
        StageHunk(line);
        ViewStage = true;
        var patch = GetDocument<PatchDocument>();
       
        AssertPatch(expectedPatch, patch);
    }
    
    [Fact]
    public void Patching_Unstage_Line()
    {
        var original = """
            line 1
            line 2
            line 3
            """;
        
        var changed = """
            line 1
            line 3
            """;

        var line = "-line 2";

        var expectedPatch = """
            -line 2
            """;
        
        WriteTheFile(original);
        StageTheFile();
        Commit();
        WriteTheFile(changed);
        StageTheFile();
        
        ViewStage = true;
        UnstageLine(line);

        var staged = GetDocument<PatchDocument>();
        AssertPatchEmpty(staged);
        
        ViewStage = false;
        var unstaged = GetDocument<PatchDocument>();
        AssertPatch(expectedPatch, unstaged);
    }
    
    [Fact]
    public void Patching_Unstage_Hunk()
    {
        var original = """
            line 1
            line 2
            line 3
            line 4
            """;
        
        var changed = """
            line 1
            line 4
            """;

        var line = "-line 2";

        var expectedPatch = """
            -line 2
            -line 3
            """;
        
        WriteTheFile(original);
        StageTheFile();
        Commit();
        WriteTheFile(changed);
        StageTheFile();
        
        ViewStage = true;
        UnstageHunk(line);

        var staged = GetDocument<PatchDocument>();
        AssertPatchEmpty(staged);
        
        ViewStage = false;
        var unstaged = GetDocument<PatchDocument>();
        AssertPatch(expectedPatch, unstaged);
    }
    
    [Fact]
    public void Patching_Reset_Line()
    {
        var original = """
            line 1
            line 2
            line 3
            """;
        
        var changed = """
            line 1
            line 3
            """;

        var line = "-line 2";
        
        WriteTheFile(original);
        StageTheFile();
        Commit();
        WriteTheFile(changed);
        
        ResetLine(line);

        var actualContents = ReadTheFile();
        Assert.Equal(original, actualContents);
        
        var unstaged = GetDocument<PatchDocument>();
        AssertPatchEmpty(unstaged);

        ViewStage = true;
        
        var staged = GetDocument<PatchDocument>();
        AssertPatchEmpty(staged);
    }
        
    [Fact]
    public void Patching_Reset_Hunk()
    {
        var original = """
            line 1
            line 2
            line 3
            line 4
            """;
        
        var changed = """
            line 1
            line 4
            """;

        var line = "-line 2";
        
        WriteTheFile(original);
        StageTheFile();
        Commit();
        WriteTheFile(changed);
        
        ResetHunk(line);

        var actualContents = ReadTheFile();
        Assert.Equal(original, actualContents);
        
        var unstaged = GetDocument<PatchDocument>();
        AssertPatchEmpty(unstaged);

        ViewStage = true;
        
        var staged = GetDocument<PatchDocument>();
        AssertPatchEmpty(staged);
    }

    [Fact]
    public void Patching_StageHunk_With_LinedEnding_Mixed()
    {
        var original = "line 1\r\nline 2\nline 3\r\nline 4\n";
        var changed = "line 1\r\nline 4\n";
        var line = "-line 2";
        
        var expectedPatch = """
            -line 2
            -line 3
            """;

        SetCoreAutoCrLfFalse();
        WriteTheFile(original);
        StageTheFile();
        Commit();
        WriteTheFile(changed);
        
        StageHunk(line);

        var unstaged = GetDocument<PatchDocument>();
        AssertPatchEmpty(unstaged);
        
        ViewStage = true;
        var staged = GetDocument<PatchDocument>();
        AssertPatch(expectedPatch, staged);
    }
    
    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Patching_StageHunk_With_LinedEnding(string lineEnding)
    {
        var original = $"line 1{lineEnding}line 2{lineEnding}line 3{lineEnding}line 4{lineEnding}";
        var changed = $"line 1{lineEnding}line 4{lineEnding}";
        var line = "-line 2";
        
        var expectedPatch = """
            -line 2
            -line 3
            """;

        SetCoreAutoCrLfFalse();
        WriteTheFile(original);
        StageTheFile();
        Commit();
        WriteTheFile(changed);
        
        StageHunk(line);

        var unstaged = GetDocument<PatchDocument>();
        AssertPatchEmpty(unstaged);
        
        ViewStage = true;
        var staged = GetDocument<PatchDocument>();
        AssertPatch(expectedPatch, staged);
    }
}