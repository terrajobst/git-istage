using GitIStage.UI;
using LibGit2Sharp;

namespace GitIStage.Services;

internal sealed class DocumentService
{
    private readonly GitService _gitService;

    private bool _viewFiles;
    private bool _viewStage;
    private bool _fullFileDiff;
    private int _contextLines = 3;

    private Document _document = Document.Empty;

    public DocumentService(GitService gitService)
    {
        _gitService = gitService;
        _gitService.RepositoryChanged += GitServiceOnRepositoryChanged;
        UpdateDocument();
    }

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
                    UpdateDocument();
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
                    UpdateDocument();
            }
        }
    }

    public void UpdateDocument()
    {
        var tipTree = _gitService.Repository.Head.Tip?.Tree;

        var compareOptions = new CompareOptions();
        compareOptions.ContextLines = _fullFileDiff ? int.MaxValue : _contextLines;

        var patch = _viewStage
            ? _gitService.Repository.Diff.Compare<Patch>(tipTree, DiffTargets.Index, null, null, compareOptions)
            : _gitService.Repository.Diff.Compare<Patch>(null, true, null, compareOptions);

        if (_viewFiles)
        {
            _document = FileDocument.Create(patch, _viewStage);
        }
        else
        {
            _document = PatchDocument.Create(patch, _viewStage);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void GitServiceOnRepositoryChanged(object? sender, EventArgs e)
    {
        UpdateDocument();
    }

    public event EventHandler? Changed;
}