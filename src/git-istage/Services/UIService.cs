using System.Text;
using GitIStage.Patches;
using GitIStage.UI;
using Microsoft.Extensions.DependencyInjection;

namespace GitIStage.Services;

internal sealed class UIService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly GitService _gitService;
    private readonly DocumentService _documentService;

    private Label _header = null!;
    private View _view = null!;
    private Label _footer = null!;
    private bool _helpShowing;
    private int _selectedLineBeforeHelpWasShown;
    private int _topLineBeforeHelpWasShown;

    private readonly StringBuilder _inputLineDigits = new();

    public UIService(IServiceProvider serviceProvider, GitService gitService, DocumentService documentService)
    {
        _serviceProvider = serviceProvider;
        _gitService = gitService;
        _documentService = documentService;
        _documentService.Changed += DocumentServiceOnChanged;
        Terminal.WindowSizeChanged += (_, _) => ResizeScreen();
    }

    public bool HelpShowing
    {
        get => _helpShowing;
        set
        {
            if (value)
                ShowHelp();
            else
                HideHelp();
        }
    }

    public View View => _view;

    public void Show()
    {
        if (OperatingSystem.IsWindows())
            Win32Console.Initialize();

        Vt100.HideCursor();
        Vt100.SwitchToAlternateBuffer();

        ResizeScreen();
    }

    public void Hide()
    {
        Vt100.ResetScrollMargins();
        Vt100.SwitchToMainBuffer();
        Vt100.ShowCursor();
        
        if (OperatingSystem.IsWindows())
            Win32Console.Restore();
    }

    public void ResizeScreen()
    {
        var oldView = (View?)_view;

        _header = new Label(0, 0, Terminal.WindowWidth);
        _header.Foreground = ConsoleColor.Yellow;
        _header.Background = ConsoleColor.DarkGray;

        _view = new View(1, 0, Terminal.WindowHeight - 1, Terminal.WindowWidth);
        _view.SelectionChanged += delegate { UpdateHeader(); };

        _footer = new Label(Terminal.WindowHeight - 1, 0, Terminal.WindowWidth);
        _footer.Foreground = ConsoleColor.Yellow;
        _footer.Background = ConsoleColor.DarkGray;

        Vt100.SetScrollMargins(2, Terminal.WindowHeight - 1);

        UpdateRepositoryState();

        if (oldView is not null)
        {
            _view.VisibleWhitespace = oldView.VisibleWhitespace;
            _view.SelectedLine = oldView.SelectedLine;
            _view.BringIntoView(_view.SelectedLine);
        }
    }

    private void DocumentServiceOnChanged(object? sender, EventArgs e)
    {
        UpdateRepositoryState();
    }

    private void UpdateRepositoryState()
    {
        _view.Document = _documentService.Document;
        UpdateHeader();
        UpdateFooter();
    }

    private void UpdateHeader()
    {
        if (_helpShowing)
        {
            _header.Text = " Keyboard shortcuts";
            return;
        }

        var mode = _documentService.ViewStage ? "S" : "W";

        if (_documentService.ViewFiles || _documentService.Document.Height == 0)
        {
            _header.Text = $" {mode} | Files ";
        }
        else
        {
            var document = (PatchDocument)_documentService.Document;
            var entryIndex = document.FindEntryIndex(_view.SelectedLine);
            var entry = entryIndex < 0 ? null : document.Patch.Entries[entryIndex];
            var emptyMarker = _documentService.ViewStage ? "*nothing to commit*" : "*clean*";
            var path = entry is null ? emptyMarker : entry.NewPath;
            _header.Text = $" {mode} | {path}";
        }
    }

    private void UpdateFooter()
    {
        if (_helpShowing)
        {
            _footer.Text = string.Empty;
            return;
        }

        var (stageAdded, stageModified, stageDeleted) = _documentService.IndexPatch.GetFileStatistics();
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
            Vt100.SetCursorPosition(0, Terminal.WindowHeight - 1);
            Vt100.SetForegroundColor(ConsoleColor.Blue);
            Vt100.SetBackgroundColor(ConsoleColor.Gray);
            Terminal.Write("/");
            Terminal.Write(sb.ToString());
            Vt100.EraseRestOfCurrentLine();
            Vt100.ShowCursor();

            var k = Terminal.ReadKey();

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
                if (sb.Length == Terminal.WindowWidth - 1)
                    break;
            }
        }

        Vt100.HideCursor();
        UpdateFooter();

        if (sb.Length == 0)
            return;

        var searchResults = new SearchResults(_view.Document, sb.ToString());
        if (searchResults.Hits.Count == 0)
        {
            Vt100.HideCursor();
            Vt100.SetCursorPosition(0, Terminal.WindowHeight - 1);
            Vt100.SetForegroundColor(ConsoleColor.Blue);
            Vt100.SetBackgroundColor(ConsoleColor.Gray);
            Terminal.Write("<< NO RESULTS FOUND >>");
            Vt100.EraseRestOfCurrentLine();
            Terminal.ReadKey();
            UpdateFooter();
            return;
        }

        _view.SearchResults = searchResults;
        _view.SelectedLine = searchResults.Hits.First().LineIndex;
    }

    public bool HasInputLine => _inputLineDigits.Length > 0;

    public void AppendLineDigit(char digit)
    {
        if (_helpShowing) return;
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

    private void ShowHelp()
    {
        var commands = _serviceProvider.GetRequiredService<CommandService>().Commands;

        _selectedLineBeforeHelpWasShown = _view.SelectedLine;
        _topLineBeforeHelpWasShown = _view.TopLine;

        _view.Document = HelpDocument.Create(commands);

        _helpShowing = true;

        UpdateHeader();
        UpdateFooter();

        _view.SelectedLine = 0;
        _view.TopLine = 0;
    }

    private void HideHelp()
    {
        _helpShowing = false;

        UpdateRepositoryState();

        _view.SelectedLine = _selectedLineBeforeHelpWasShown == -1 ? 0 : _selectedLineBeforeHelpWasShown;
        _view.TopLine = _topLineBeforeHelpWasShown;
    }

    public void RenderGitError(GitCommandFailedException ex)
    {
        Terminal.Clear();
        Terminal.WriteLine(ex.Message);
        Terminal.ReadKey();
    }
}