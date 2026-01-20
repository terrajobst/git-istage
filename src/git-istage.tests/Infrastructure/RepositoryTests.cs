using System.Text.RegularExpressions;
using GitIStage.UI;
using LibGit2Sharp;

namespace GitIStage.Tests.Infrastructure;

public abstract class RepositoryTests : IDisposable
{
    private readonly string _tempPath;
    private readonly GitService _gitService;
    private readonly DocumentService _documentService;
    private readonly PatchingService _patchingService;

    private bool _wroteToWorkingDirectory;

    protected RepositoryTests()
    {
        _tempPath = Path.Join(Path.GetTempPath(), "git-istage-test_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempPath);
        Repository.Init(_tempPath, isBare: false);

        var gitEnvironment = new GitEnvironment(repositoryPath: _tempPath);
        _gitService = new GitService(gitEnvironment);
        _documentService = new DocumentService(_gitService);
        _patchingService = new PatchingService(_gitService, _documentService);
    }

    public void Dispose()
    {
        _gitService.Dispose();

        // Directory.Delete() will fail for any read only files. Need to reset attributes first.
        foreach (var file in Directory.EnumerateFiles(_tempPath, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);

        Directory.Delete(_tempPath, recursive: true);
    }

    internal void SetCoreAutoCrLfFalse()
    {
        _gitService.Repository.Config.Set("core.autocrlf", false);
    }

    internal void WriteTheFile(string contents)
    {
        WriteFile("file.txt", contents);
    }

    internal void StageTheFile()
    {
        StageFile("file.txt");
    }

    internal void TouchFile(string fileName)
    {
        var fullPath = Path.Combine(_tempPath, fileName);
        File.WriteAllText(fullPath, string.Empty);
        _wroteToWorkingDirectory = true;
    }

    internal void WriteFile(string fileName, string contents)
    {
        var fullPath = Path.Combine(_tempPath, fileName);
        File.WriteAllText(fullPath, contents);
        _wroteToWorkingDirectory = true;
    }

    internal void DeleteFile(string fileName)
    {
        var fullPath = Path.Combine(_tempPath, fileName);
        File.Exists(fullPath).Should().BeTrue();
        File.Delete(fullPath);
        _wroteToWorkingDirectory = true;
    }

    internal string ReadTheFile()
    {
        return ReadFile("file.txt");
    }

    internal string ReadFile(string fileName)
    {
        var fullPath = Path.Combine(_tempPath, fileName);
        return File.ReadAllText(fullPath);
    }

    internal void StageAll()
    {
        var changes = _gitService.Repository.Diff.Compare<TreeChanges>(null, true);
        foreach (var change in changes)
            LibGit2Sharp.Commands.Stage(_gitService.Repository, change.OldPath);
    }

    internal void StageFile(string fileName)
    {
        LibGit2Sharp.Commands.Stage(_gitService.Repository, fileName);
    }

    internal void Commit()
    {
        var tipTree = _gitService.Repository.Head.Tip?.Tree;
        var changes = _gitService.Repository.Diff.Compare<TreeChanges>(tipTree, DiffTargets.Index);
        if (!changes.Any())
            throw new Exception($"Nothing to commit -- did you forget to stage changes?");

        var signature = new Signature("git-istage", "git_istage@example.org", DateTimeOffset.Now);
        _gitService.Repository.Commit("Update", signature, signature);
    }

    internal bool ViewFiles { get; set; }

    internal bool ViewStage { get; set; }

    [CustomAssertion]
    internal T GetDocument<T>()
        where T : Document
    {
        EnsureDocumentIsUpToDate();
        return GetUntypedDocument().Should().BeAssignableTo<T>().Subject;

        Document GetUntypedDocument()
        {
            if (ViewStage)
            {
                return ViewFiles ? _documentService.StageFilesDocument : _documentService.StagePatchDocument;
            }
            else
            {
                return ViewFiles ? _documentService.WorkingCopyFilesDocument : _documentService.WorkingCopyPatchDocument;
            }
        }
    }

    internal Document GetDocument() => GetDocument<Document>();

    private void EnsureDocumentIsUpToDate()
    {
        _documentService.RecomputePatch();

        if (!_wroteToWorkingDirectory)
            return;

        _wroteToWorkingDirectory = false;
        _documentService.UpdateDocument();
    }

    internal void StageLine(string line)
    {
        Apply(PatchDirection.Stage, line, entireHunk: false);
    }

    internal void StageHunk(string line)
    {
        Apply(PatchDirection.Stage, line, entireHunk: true);
    }

    internal void UnstageLine(string line)
    {
        Apply(PatchDirection.Unstage, line, entireHunk: false);
    }

    internal void UnstageHunk(string line)
    {
        Apply(PatchDirection.Unstage, line, entireHunk: true);
    }

    internal void ResetLine(string line)
    {
        Apply(PatchDirection.Reset, line, entireHunk: false);
    }

    internal void ResetHunk(string line)
    {
        Apply(PatchDirection.Reset, line, entireHunk: true);
    }

    private void Apply(PatchDirection direction, string wildcardPattern, bool entireHunk)
    {
        EnsureDocumentIsUpToDate();

        var document = GetDocument();
        var regex = WildcardPatternToRegex(wildcardPattern);
        var matchingLineIndices = Enumerable
                                  .Range(0, document.Height)
                                  .Where(i => Regex.IsMatch(document.GetLine(i), regex));

        var selectedLineIndex = matchingLineIndices.Should().ContainSingle().Subject;
        _patchingService.ApplyPatch(document, direction, entireHunk, selectedLineIndex);

        static string WildcardPatternToRegex(string pattern)
        {
            return "^"
                   + Regex.Escape(pattern)
                          .Replace("\\*", ".*", StringComparison.Ordinal)
                          .Replace("\\?", ".", StringComparison.Ordinal)
                   + "$";
        }
    }

    internal void AssertPatch(string expectedPatch, PatchDocument actualPatch, bool includeNoFinalLineBreaks = false)
    {
        var actualPatchLines = actualPatch.Patch.Lines
                                          .Where(l => l.Kind.IsAddedOrDeletedLine() ||
                                                      includeNoFinalLineBreaks && l.Kind ==PatchNodeKind.NoFinalLineBreakLine)
                                          .Select(l => l.Text.ToString());

        var expectedPatchLines = expectedPatch.ReplaceLineEndings("\n")
            .Split('\n');

        actualPatchLines.Should().Equal(expectedPatchLines);
    }

    internal void AssertPatchEmpty(PatchDocument patch)
    {
        Assert.Empty(patch.Patch.Lines);
    }

    internal static void AssertFiles(string expectedChangesText, FileDocument document)
    {
        var expectedChangeLines = expectedChangesText
            .ReplaceLineEndings(Environment.NewLine)
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var actualChangeLines = new List<string>();
        for (var i = 0; i < document.Height; i++)
        {
            var line = document.GetLine(i);
            var colon = line.IndexOf(':');
            if (colon < 0)
                continue;

            var changeText = line.Slice(0, colon).Trim();
            var path = line.Slice(colon + 1).Trim();

            if (changeText.Length == 0 || path.Length == 0)
                continue;

            var marker = changeText switch
            {
                "added" => "+",
                "modified" => "~",
                "deleted" => "-",
                _ => null
            };

            if (marker is null)
                continue;

            actualChangeLines.Add($"{marker} {path}");
        }

        actualChangeLines.Should().BeEquivalentTo(expectedChangeLines);
    }

    internal void AssertFilesEmpty(FileDocument document)
    {
        document.Patch.Entries.Should().BeEmpty();
    }
}