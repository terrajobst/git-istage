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
    private readonly OperationLogService _logService;
    private readonly ThemeService _themeService;

    private readonly Label _header;
    private readonly Label _footer;

    private ViewMode _viewMode;
    private ViewMode _previousViewMode;
    private View _activeView;
    private readonly View _workingCopyPatchView;
    private readonly View _workingCopyFilesView;
    private readonly View _stagePatchView;
    private readonly View _stageFilesView;
    private readonly View _logView;
    private readonly View _helpView;
    private HelpDocument? _helpDocument;

    private readonly StringBuilder _inputLineDigits = new();

    public UIService(IServiceProvider serviceProvider,
                     KeyboardService keyboardService,
                     GitService gitService,
                     DocumentService documentService,
                     OperationLogService logService,
                     ThemeService themeService)
    {
        _serviceProvider = serviceProvider;
        _keyboardService = keyboardService;
        _gitService = gitService;
        _documentService = documentService;
        _documentService.Changed += DocumentServiceOnChanged;
        _logService = logService;
        _logService.Changed += LogServiceOnChanged;
        _themeService = themeService;
        _themeService.ThemeChanged += ThemeServiceOnChanged;

        _header = new Label();
        _header.Foreground = _themeService.Colors.HeaderForeground;
        _header.Background = _themeService.Colors.HeaderBackground;

        _footer = new Label();
        _footer.Foreground = _themeService.Colors.HeaderForeground;
        _footer.Background = _themeService.Colors.HeaderBackground;

        _workingCopyPatchView = new View(themeService);
        _workingCopyFilesView = new View(themeService);
        _stagePatchView = new View(themeService);
        _stageFilesView = new View(themeService);
        _logView = new View(themeService);
        _helpView = new View(themeService);
        _activeView = _workingCopyPatchView;

        UpdatePatchDocuments();
        UpdateActiveView();
    }

    public bool HelpShowing
    {
        get => ViewMode == ViewMode.Help;
        set => ViewMode = value ? ViewMode.Help : _previousViewMode;
    }

    public bool LogShowing
    {
        get => ViewMode == ViewMode.Log;
        set => ViewMode = value ? ViewMode.Log : _previousViewMode;
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

        Console.HideCursor();
        Console.SwitchToAlternateBuffer();

        Resize();
    }

    public void Hide()
    {
        Console.SwitchToMainBuffer();
        Console.ShowCursor();

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
        _logView.Resize(viewTop, viewLeft, viewHeight, viewWidth);
        _helpView.Resize(viewTop, viewLeft, viewHeight, viewWidth);

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
            case ViewMode.Log:
                _activeView = _logView;
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

    private void ThemeServiceOnChanged(object? sender, EventArgs e)
    {
        _header.Foreground = _themeService.Colors.HeaderForeground;
        _header.Background = _themeService.Colors.HeaderBackground;
        _footer.Foreground = _themeService.Colors.HeaderForeground;
        _footer.Background = _themeService.Colors.HeaderBackground;
        UpdateHeaderAndFooter();
    }

    private void DocumentServiceOnChanged(object? sender, EventArgs e)
    {
        UpdatePatchDocuments();
        UpdateHeaderAndFooter();
    }

    private void LogServiceOnChanged(object? sender, EventArgs e)
    {
        _logView.Document = _logService.Document;

        if (_logService.LastUpdateHadErrors)
            LogShowing = true;
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

        if (LogShowing)
        {
            _header.Text = " Operation Log";
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
        if (HelpShowing || LogShowing)
        {
            _footer.Text = string.Empty;
            return;
        }

        var (stageAdded, stageModified, stageDeleted) = _documentService.StagePatch.GetFileStatistics();
        var (workingAdded, workingModified, workingDeleted) = _documentService.WorkingCopyPatch.GetFileStatistics();

        var lineNumberText = _inputLineDigits.Length > 0 ? $"L{_inputLineDigits}" : "";
        var leftPart = $" [{_gitService.Repository.Head.FriendlyName} +{stageAdded} ~{stageModified} -{stageDeleted} | +{workingAdded} ~{workingModified} -{workingDeleted}]    {lineNumberText}";
        var rightPart = $"{_themeService.ThemeName} ";
        var padding = Math.Max(0, _footer.Width - leftPart.Length - rightPart.Length);

        _footer.Text = $"{leftPart}{new string(' ', padding)}{rightPart}";
    }

    public void RenderAll()
    {
        var buffer = RenderBuffer.Begin();
        _header.Render(buffer);
        _footer.Render(buffer);
        if (_activeView.Visible)
            _activeView.Render(buffer);
        buffer.Flush();
    }

    public void Search()
    {
        var sb = new StringBuilder();

        while (true)
        {
            Console.HideCursor();
            var buffer = RenderBuffer.Begin();
            buffer.SetCursorPosition(0, Console.WindowHeight - 1);
            buffer.SetForegroundColor(_themeService.Colors.SearchInputForeground);
            buffer.SetBackgroundColor(_themeService.Colors.SearchInputBackground);
            buffer.Write('/');
            buffer.Write(sb.ToString());
            buffer.EraseRestOfCurrentLine();
            buffer.Flush();
            Console.ShowCursor();

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

        Console.HideCursor();
        UpdateFooter();

        if (sb.Length == 0)
        {
            RenderAll();
            return;
        }

        var searchResults = new SearchResults(_activeView.Document, sb.ToString());
        if (searchResults.Hits.Count == 0)
        {
            Console.HideCursor();
            var noResultsBuffer = RenderBuffer.Begin();
            noResultsBuffer.SetCursorPosition(0, Console.WindowHeight - 1);
            noResultsBuffer.SetForegroundColor(_themeService.Colors.SearchInputForeground);
            noResultsBuffer.SetBackgroundColor(_themeService.Colors.SearchInputBackground);
            noResultsBuffer.Write("<< NO RESULTS FOUND >>");
            noResultsBuffer.EraseRestOfCurrentLine();
            noResultsBuffer.Flush();
            _keyboardService.ReadKey();
            UpdateFooter();
            RenderAll();
            return;
        }

        _activeView.SearchResults = searchResults;
        _activeView.SelectedLine = searchResults.Hits.First().LineIndex;
        RenderAll();
    }

    public bool HasInputLine => _inputLineDigits.Length > 0;

    public void AppendLineDigit(char digit)
    {
        if (HelpShowing || LogShowing)
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
}
