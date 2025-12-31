using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using GitIStage.Patches;
using GitIStage.Text;
using GitIStage.UI;
using LibGit2Sharp;

using GitIStagePatch = GitIStage.Patches.Patch;
using Patch = LibGit2Sharp.Patch;

namespace GitIStage.Services;

internal sealed class DocumentService
{
    private readonly GitService _gitService;

    private GitIStagePatch _workingCopyPatch;
    private GitIStagePatch _indexPatch;
    private bool _viewFiles;
    private bool _viewStage;
    private bool _fullFileDiff;
    private int _contextLines = 3;

    private Document _document = Document.Empty;

    public DocumentService(GitService gitService)
    {
        _gitService = gitService;
        _gitService.RepositoryChanged += GitServiceOnRepositoryChanged;
        RecomputePatch();
    }

    public GitIStagePatch WorkingCopyPatch => _workingCopyPatch;

    public GitIStagePatch IndexPatch => _indexPatch;

    public Document Document => _document;

    public bool ViewFiles
    {
        get => _viewFiles;
        set
        {
            if (_viewFiles != value)
            {
                _viewFiles = value;
                UpdateDocument();
            }
        }
    }

    public bool ViewStage
    {
        get => _viewStage;
        set
        {
            if (_viewStage != value)
            {
                _viewStage = value;
                UpdateDocument();
            }
        }
    }

    public bool ViewFullDiff
    {
        get => _fullFileDiff;
        set
        {
            if (_fullFileDiff != value)
            {
                _fullFileDiff = value;
                if (!_viewFiles)
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
                if (!_viewFiles)
                    RecomputePatch();
            }
        }
    }

    [MemberNotNull(nameof(_workingCopyPatch))]
    [MemberNotNull(nameof(_indexPatch))]
    public void RecomputePatch()
    {
        _workingCopyPatch = GitIStagePatch.Parse(GetPatch(stage: false));
        _indexPatch = GitIStagePatch.Parse(GetPatch(stage: true));
        UpdateDocument();
    }

    private void UpdatePatch(ImmutableArray<string> affectedPaths)
    {
        var affectedPathSet = new HashSet<string>(affectedPaths);
        var patch = GitIStagePatch.Parse(GetPatch(stage: false, affectedPaths));

        var newPatchEntryByPath = patch.Entries.ToDictionary(GetPath);
        var entries = new List<PatchEntry>();

        foreach (var oldEntry in _workingCopyPatch.Entries)
        {
            var path = GetPath(oldEntry);
            if (newPatchEntryByPath.TryGetValue(path, out var newEntry))
                entries.Add(newEntry);
            else if (!affectedPathSet.Contains(path))
                entries.Add(oldEntry);
        }

        var sb = new StringBuilder();
        foreach (var entry in entries)
            WriteEntry(sb, entry);

        _workingCopyPatch = GitIStagePatch.Parse(sb.ToString());
        _indexPatch = GitIStagePatch.Parse(GetPatch(stage: true));
        UpdateDocument();

        static string GetPath(PatchEntry e)
        {
            return string.IsNullOrEmpty(e.NewPath) ? e.OldPath : e.NewPath;
        }

        static void WriteEntry(StringBuilder sb, PatchEntry e)
        {
            var text = e.Root.Text;
            var firstLine = text.GetLineIndex(e.Span.Start);
            var lastLine = text.GetLineIndex(e.Span.End);
            var start = text.Lines[firstLine].Start;
            var end = text.Lines[lastLine].SpanIncludingLineBreak.End;
            var span = TextSpan.FromBounds(start, end);
            sb.Append(text.AsSpan(span));
        }
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

    public void UpdateDocument()
    {
        var patch = _viewStage ? _indexPatch : _workingCopyPatch;
        
        if (_viewFiles)
        {
            _document = FileDocument.Create(patch, _viewStage);
        }
        else
        {
            _document = PatchDocument.Create(patch);
        }

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
