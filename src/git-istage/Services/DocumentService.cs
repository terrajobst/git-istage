using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using GitIStage.Patches;
using GitIStage.UI;
using GitIStagePatch = GitIStage.Patches.Patch;

namespace GitIStage.Services;

internal sealed class DocumentService
{
    private readonly GitService _gitService;

    private GitIStagePatch _workingCopyPatch;
    private GitIStagePatch _stagePatch;
    private bool _fullFileDiff;
    private int _contextLines = 3;

    private PatchDocument _workingCopyPatchDocument;
    private FileDocument _workingCopyFilesDocument;
    private PatchDocument _stagePatchDocument;
    private FileDocument _stageFilesDocument;

    public DocumentService(GitService gitService, FileWatchingService? fileWatchingService)
    {
        _gitService = gitService;
        _gitService.RepositoryChanged += GitServiceOnRepositoryChanged;
        fileWatchingService?.Changed += FileWatchingServiceOnChanged;
        RecomputePatch();
    }

    public GitIStagePatch WorkingCopyPatch => _workingCopyPatch;

    public GitIStagePatch StagePatch => _stagePatch;

    public PatchDocument WorkingCopyPatchDocument => _workingCopyPatchDocument;

    public FileDocument WorkingCopyFilesDocument => _workingCopyFilesDocument;

    public PatchDocument StagePatchDocument => _stagePatchDocument;

    public FileDocument StageFilesDocument => _stageFilesDocument;

    public bool ViewFullDiff
    {
        get => _fullFileDiff;
        set
        {
            if (_fullFileDiff != value)
            {
                _fullFileDiff = value;
                RecomputePatch();
            }
        }
    }

    public int ContextLines
    {
        get => _contextLines;
        set
        {
            if (_contextLines != value)
            {
                _contextLines = value;
                RecomputePatch();
            }
        }
    }

    [MemberNotNull(nameof(_workingCopyPatch))]
    [MemberNotNull(nameof(_stagePatch))]
    [MemberNotNull(nameof(_workingCopyPatchDocument))]
    [MemberNotNull(nameof(_workingCopyFilesDocument))]
    [MemberNotNull(nameof(_stagePatchDocument))]
    [MemberNotNull(nameof(_stageFilesDocument))]
    public void RecomputePatch()
    {
        _workingCopyPatch = GetWorkingCopyPatch();
        _stagePatch = GetStagePatch();
        UpdateDocument();
    }

    private void UpdatePatch(ImmutableArray<string> updatedPaths,
                             bool skipIndex = false)
    {
        var patchForUpdatedPaths = GetWorkingCopyPatch(updatedPaths);
        var result = _workingCopyPatch.Update(patchForUpdatedPaths, updatedPaths);

        _workingCopyPatch = result;
        if (!skipIndex)
            _stagePatch = GetStagePatch();
        UpdateDocument();
    }

    private GitIStagePatch GetWorkingCopyPatch(IEnumerable<string>? affectedPaths = null)
    {
        return GitIStagePatch.Parse(_gitService.GetPatch(_fullFileDiff, _contextLines, stage: false, affectedPaths));
    }

    private GitIStagePatch GetStagePatch()
    {
        return GitIStagePatch.Parse(_gitService.GetPatch(_fullFileDiff, _contextLines, stage: true));
    }

    [MemberNotNull(nameof(_workingCopyPatchDocument))]
    [MemberNotNull(nameof(_workingCopyFilesDocument))]
    [MemberNotNull(nameof(_stagePatchDocument))]
    [MemberNotNull(nameof(_stageFilesDocument))]
    public void UpdateDocument()
    {
        _workingCopyPatchDocument = PatchDocument.Create(_workingCopyPatch);
        _workingCopyFilesDocument = FileDocument.Create(_workingCopyPatch, viewStage: false);
        _stagePatchDocument = PatchDocument.Create(_stagePatch);
        _stageFilesDocument = FileDocument.Create(_stagePatch, viewStage: true);

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void GitServiceOnRepositoryChanged(object? sender, RepositoryChangedEventArgs e)
    {
        if (e.AffectedPaths.Any())
            UpdatePatch(e.AffectedPaths);
        else
            RecomputePatch();
    }

    private void FileWatchingServiceOnChanged(object? sender, FileWatchingEventArgs args)
    {
        var workingDirectory = _gitService.Repository.Info.WorkingDirectory;
        var repositoryDirectory = _gitService.Repository.Info.Path;

        var pathsInWorkingCopyPatch = _workingCopyPatch
            .Entries
            .Select(e => Path.GetFullPath(Path.Join(workingDirectory, e.NewPath)))
            .ToHashSet(StringComparer.Ordinal);

        var entriesToAddOrUpdate = new SortedSet<string>(StringComparer.Ordinal);
        var indexWasChanged = false;

        foreach (var @event in args.Events)
        {
            if (@event is not RenamedEventArgs rename)
            {
                if (!_gitService.IsIgnoredOrOutsideWorkingDirectory(@event.FullPath))
                {
                    if (File.Exists(@event.FullPath))
                        entriesToAddOrUpdate.Add(@event.FullPath);
                }
            }
            else
            {
                var isChangeInGitDirectory = rename.OldFullPath.StartsWith(repositoryDirectory, StringComparison.Ordinal);
                if (isChangeInGitDirectory)
                {
                    var isIndexChange = string.Equals(rename.OldFullPath, Path.Join(repositoryDirectory, "index.lock"), StringComparison.Ordinal) &&
                                        string.Equals(rename.FullPath, Path.Join(repositoryDirectory, "index"), StringComparison.Ordinal);

                    if (isIndexChange)
                        indexWasChanged = true;
                }
                else
                {
                    AddRename(rename.OldFullPath);
                    AddRename(rename.FullPath);

                    foreach (var affectedOldPath in pathsInWorkingCopyPatch.Where(p => p.StartsWith(rename.OldFullPath, StringComparison.Ordinal)))
                    {
                        var suffix = affectedOldPath.Substring(rename.OldFullPath.Length);
                        var affectedNewPath = rename.FullPath + suffix;

                        AddRename(affectedOldPath);
                        AddRename(affectedNewPath);
                    }

                    void AddRename(string path)
                    {
                        if (!_gitService.IsIgnoredOrOutsideWorkingDirectory(path))
                            entriesToAddOrUpdate.Add(path);
                    }
                }
            }
        }

        if (indexWasChanged)
        {
            _gitService.InitializeRepository();
            var stagePatchOld = _stagePatch;
            _stagePatch = GetStagePatch();

            var beforePaths = stagePatchOld.Entries.Select(e => Path.GetFullPath(Path.Join(workingDirectory, e.NewPath))).ToHashSet(StringComparer.Ordinal);
            var afterPaths = _stagePatch.Entries.Select(e => Path.GetFullPath(Path.Join(workingDirectory, e.NewPath))).ToHashSet(StringComparer.Ordinal);

            var newPaths = afterPaths.Except(beforePaths, StringComparer.Ordinal);
            var removedPaths = beforePaths.Except(afterPaths, StringComparer.Ordinal);

            entriesToAddOrUpdate.UnionWith(newPaths);
            entriesToAddOrUpdate.UnionWith(removedPaths);
        }

        ToRepoPaths(ref entriesToAddOrUpdate, workingDirectory);

        static void ToRepoPaths(ref SortedSet<string> set, string workingDirectory)
        {
            set = new SortedSet<string>(set.Select(p => Path.GetRelativePath(workingDirectory, p).Replace(Path.DirectorySeparatorChar, '/')));
        }

        if (entriesToAddOrUpdate.Count > 0)
        {
            UpdatePatch([..entriesToAddOrUpdate], skipIndex: indexWasChanged);
        }
    }

    public event EventHandler? Changed;
}
