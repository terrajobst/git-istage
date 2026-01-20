using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using GitIStage.Patches;
using GitIStage.UI;
using LibGit2Sharp;

using GitIStagePatch = GitIStage.Patches.Patch;
using Patch = LibGit2Sharp.Patch;

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

    public DocumentService(GitService gitService)
    {
        _gitService = gitService;
        _gitService.RepositoryChanged += GitServiceOnRepositoryChanged;
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
        _workingCopyPatch = GitIStagePatch.Parse(GetPatch(stage: false));
        _stagePatch = GitIStagePatch.Parse(GetPatch(stage: true));
        UpdateDocument();
    }

    private void UpdatePatch(ImmutableArray<string> affectedPaths)
    {
        var affectedPathSet = affectedPaths.ToHashSet();
        var patchForAffectedPaths = GitIStagePatch.Parse(GetPatch(stage: false, affectedPathSet));
        var result = _workingCopyPatch.Update(affectedPathSet, patchForAffectedPaths);

        _workingCopyPatch = result;
        _stagePatch = GitIStagePatch.Parse(GetPatch(stage: true));
        UpdateDocument();
    }

    private string GetPatch(bool stage, IEnumerable<string>? affectedPaths = null)
    {
        var tipTree = _gitService.Repository.Head.Tip?.Tree;

        var compareOptions = new CompareOptions();
        compareOptions.ContextLines = _fullFileDiff ? int.MaxValue : _contextLines;

        return stage
            ? _gitService.Repository.Diff.Compare<Patch>(tipTree, DiffTargets.Index, affectedPaths, null, compareOptions)
            : _gitService.Repository.Diff.Compare<Patch>(affectedPaths, true, null, compareOptions);
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

    public event EventHandler? Changed;
}
