using System.Diagnostics;
using System.Text;
using GitIStage.Patches;
using GitIStage.UI;
using Microsoft.Extensions.DependencyInjection;

namespace GitIStage.Services;

internal sealed class UIService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KeyboardService _keyboardService;
    private readonly GitService _gitService;
    private readonly DocumentService _documentService;

    private readonly Label _header;
    private readonly Label _footer;

    private ViewMode _viewMode;
    private ViewMode _previousViewMode;
    private View _activeView;
    private readonly View _workingCopyPatchView;
    private readonly View _workingCopyFilesView;
    private readonly View _stagePatchView;
    private readonly View _stageFilesView;
    private readonly View _errorView;
    private readonly View _helpView;
    private HelpDocument? _helpDocument;

    private readonly StringBuilder _inputLineDigits = new();

    public UIService(IServiceProvider serviceProvider, KeyboardService keyboardService, GitService gitService, DocumentService documentService)
    {
        _serviceProvider = serviceProvider;
        _keyboardService = keyboardService;
        _gitService = gitService;
        _documentService = documentService;
        _documentService.Changed += DocumentServiceOnChanged;
        
        _header = new Label();
        _header.Foreground = ConsoleColor.Yellow;
        _header.Background = ConsoleColor.DarkGray;

        _footer = new Label();
        _footer.Foreground = ConsoleColor.Yellow;
        _footer.Background = ConsoleColor.DarkGray;
        
        _workingCopyPatchView = new View();
        _workingCopyFilesView = new View();
        _stagePatchView = new View();
        _stageFilesView = new View();
        _errorView = new View();
        _helpView = new View();
        _activeView = _workingCopyPatchView;

        UpdatePatchDocuments();
        UpdateActiveView();
    }

    public bool HelpShowing
    {
        get => ViewMode == ViewMode.Help;
        set => ViewMode = value ? ViewMode.Help : _previousViewMode;
    }

    public bool ErrorShowing
    {
        get => ViewMode == ViewMode.Error;
        set => ViewMode = value ? ViewMode.Error : _previousViewMode;
    }

    public bool IsViewingDiff
    {
        get => ViewMode is ViewMode.WorkingCopyPatch
                        or ViewMode.WorkingCopyFiles
                        or ViewMode.StagePatch
                        or ViewMode.StageFiles;
    }

    public bool IsViewingPatch
    {
        get => ViewMode is ViewMode.WorkingCopyPatch
                        or ViewMode.StagePatch;
    }

    public bool IsViewingWorkingCopy
    {
        get => ViewMode is ViewMode.WorkingCopyPatch
                        or ViewMode.WorkingCopyFiles;
    }

    public bool IsViewingFiles
    {
        get => ViewMode is ViewMode.WorkingCopyFiles
                        or ViewMode.StageFiles;
    }

    public bool IsViewingStage
    {
        get => ViewMode is ViewMode.StagePatch
                        or ViewMode.StageFiles;
    }

    public View View => _activeView;

    public ViewMode ViewMode
    {
        get => _viewMode;
        set
        {
            if (_viewMode != value)
            {
                _previousViewMode = _viewMode; 
                _viewMode = value;
                UpdateActiveView();
            }
        }
    }

    public void Show()
    {
        if (OperatingSystem.IsWindows())
            Win32Console.Initialize();

        Vt100.HideCursor();
        Vt100.SwitchToAlternateBuffer();

        Resize();
    }

    public void Hide()
    {
        Vt100.ResetScrollMargins();
        Vt100.SwitchToMainBuffer();
        Vt100.ShowCursor();
        
        if (OperatingSystem.IsWindows())
            Win32Console.Restore();
    }

    public void Resize()
    {
        _header.Resize(0, 0, Console.WindowWidth);
        _footer.Resize(Console.WindowHeight - 1, 0, Console.WindowWidth);
        
        var viewTop = 1;
        var viewLeft = 0;
        var viewHeight = Console.WindowHeight - 1;
        var viewWidth = Console.WindowWidth;
        _workingCopyPatchView.Resize(viewTop, viewLeft, viewHeight, viewWidth);
        _workingCopyFilesView.Resize(viewTop, viewLeft, viewHeight, viewWidth);
        _stagePatchView.Resize(viewTop, viewLeft, viewHeight, viewWidth);
        _stageFilesView.Resize(viewTop, viewLeft, viewHeight, viewWidth);
        _errorView.Resize(viewTop, viewLeft, viewHeight, viewWidth);
        _helpView.Resize(viewTop, viewLeft, viewHeight, viewWidth);
        
        Vt100.SetScrollMargins(2, Console.WindowHeight - 1);

        UpdateHeaderAndFooter();
    }

    private void UpdateActiveView()
    {
        _activeView.Visible = false;
        _activeView.SelectionChanged -= ActiveViewSelectionChanged;
        
        switch (_viewMode)
        {
            case ViewMode.WorkingCopyPatch:
                _activeView = _workingCopyPatchView;
                break;
            case ViewMode.WorkingCopyFiles:
                _activeView = _workingCopyFilesView;
                break;
            case ViewMode.StagePatch:
                _activeView = _stagePatchView;
                break;
            case ViewMode.StageFiles:
                _activeView = _stageFilesView;
                break;
            case ViewMode.Error:
                _activeView = _errorView;
                break;
            case ViewMode.Help:
                if (_helpDocument is null)
                {
                    var commands = _serviceProvider.GetRequiredService<CommandService>().Commands;
                    _helpDocument = HelpDocument.Create(commands);
                    _helpView.Document = _helpDocument;
                }
                _activeView = _helpView;
                break;
        }

        _activeView.Visible = true;
        _activeView.SelectionChanged += ActiveViewSelectionChanged;
        
        UpdateHeaderAndFooter();
    }

    private void UpdatePatchDocuments()
    {
        _workingCopyPatchView.Document = _documentService.WorkingCopyPatchDocument;
        _workingCopyFilesView.Document = _documentService.WorkingCopyFilesDocument;
        _stagePatchView.Document = _documentService.StagePatchDocument;
        _stageFilesView.Document = _documentService.StageFilesDocument;
    }
    
    private void ActiveViewSelectionChanged(object? sender, EventArgs e)
    {
        UpdateHeader();
    }

    private void DocumentServiceOnChanged(object? sender, EventArgs e)
    {
        UpdatePatchDocuments();
        UpdateHeaderAndFooter();
    }

    private void UpdateHeaderAndFooter()
    {
        UpdateHeader();
        UpdateFooter();
    }

    private void UpdateHeader()
    {
        if (HelpShowing)
        {
            _header.Text = " Keyboard shortcuts";
            return;
        }

        if (ErrorShowing)
        {
            _header.Text = " Errors";
            return;
        }

        Debug.Assert(IsViewingDiff);
        
        var mode = IsViewingStage ? "S" : "W";

        if (IsViewingFiles)
        {
            _header.Text = $" {mode} | Files ";
        }
        else
        {
            var document = (PatchDocument)_activeView.Document;
            var entryIndex = document.FindEntryIndex(_activeView.SelectedLine);
            var entry = entryIndex < 0 ? null : document.Patch.Entries[entryIndex];
            var emptyMarker = IsViewingStage ? "*nothing to commit*" : "*clean*";
            var path = entry is null ? emptyMarker : entry.NewPath;
            _header.Text = $" {mode} | {path}";
        }
    }

    private void UpdateFooter()
    {
        if (HelpShowing || ErrorShowing)
        {
            _footer.Text = string.Empty;
            return;
        }

        var (stageAdded, stageModified, stageDeleted) = _documentService.StagePatch.GetFileStatistics();
        var (workingAdded, workingModified, workingDeleted) = _documentService.WorkingCopyPatch.GetFileStatistics();

        var lineNumberText = _inputLineDigits.Length > 0 ? $"L{_inputLineDigits}" : "";

        _footer.Text = $" [{_gitService.Repository.Head.FriendlyName} +{stageAdded} ~{stageModified} -{stageDeleted} | +{workingAdded} ~{workingModified} -{workingDeleted}]    {lineNumberText} ";
    }

    public void Search()
    {
        var sb = new StringBuilder();

        while (true)
        {
            Vt100.HideCursor();
            Vt100.SetCursorPosition(0, Console.WindowHeight - 1);
            Vt100.SetForegroundColor(ConsoleColor.Blue);
            Vt100.SetBackgroundColor(ConsoleColor.Gray);
            Console.Write("/");
            Console.Write(sb);
            Vt100.EraseRestOfCurrentLine();
            Vt100.ShowCursor();

            var k = _keyboardService.ReadKey();

            if (k.Key == ConsoleKey.Enter)
            {
                break;
            }
            else if (k.Key == ConsoleKey.Escape)
            {
                sb.Clear();
                break;
            }
            else if (k.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0)
                    sb.Length--;
            }
            else if (k.KeyChar >= 32)
            {
                sb.Append(k.KeyChar);
                if (sb.Length == Console.WindowWidth - 1)
                    break;
            }
        }

        Vt100.HideCursor();
        UpdateFooter();

        if (sb.Length == 0)
            return;

        var searchResults = new SearchResults(_activeView.Document, sb.ToString());
        if (searchResults.Hits.Count == 0)
        {
            Vt100.HideCursor();
            Vt100.SetCursorPosition(0, Console.WindowHeight - 1);
            Vt100.SetForegroundColor(ConsoleColor.Blue);
            Vt100.SetBackgroundColor(ConsoleColor.Gray);
            Console.Write("<< NO RESULTS FOUND >>");
            Vt100.EraseRestOfCurrentLine();
            _keyboardService.ReadKey();
            UpdateFooter();
            return;
        }

        _activeView.SearchResults = searchResults;
        _activeView.SelectedLine = searchResults.Hits.First().LineIndex;
    }

    public bool HasInputLine => _inputLineDigits.Length > 0;

    public void AppendLineDigit(char digit)
    {
        if (HelpShowing || ErrorShowing)
            return;

        _inputLineDigits.Append(digit);
        UpdateFooter();
    }

    public void RemoveLastLineDigit()
    {
        if (_inputLineDigits.Length == 0)
            return;

        _inputLineDigits.Remove(_inputLineDigits.Length - 1, 1);
        UpdateFooter();
    }

    public bool TryGetInputLine(out int result)
    {
        if (!int.TryParse(_inputLineDigits.ToString(), out result))
            return false;

        _inputLineDigits.Clear();
        UpdateFooter();
        return true;
    }

    public void RenderGitError(GitCommandFailedException ex)
    {
        _errorView.Document = ErrorDocument.Create(ex);
        ViewMode = ViewMode.Error;
    }
}