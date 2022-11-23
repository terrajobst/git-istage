using System.Text;
using GitIStage.Patches;
using GitIStage.UI;
using LibGit2Sharp;
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

        Vt100.SwitchToAlternateBuffer();
        Vt100.HideCursor();

        ResizeScreen();
    }

    public void Hide()
    {
        Vt100.ResetScrollMargins();
        Vt100.SwitchToMainBuffer();
        Vt100.ShowCursor();
    }

    public void ResizeScreen()
    {
        var oldView = (View?)_view;

        _header = new Label(0, 0, Console.WindowWidth);
        _header.Foreground = ConsoleColor.Yellow;
        _header.Background = ConsoleColor.DarkGray;

        _view = new View(1, 0, Console.WindowHeight - 1, Console.WindowWidth);
        _view.SelectedLineChanged += delegate { UpdateHeader(); };

        _footer = new Label(Console.WindowHeight - 1, 0, Console.WindowWidth);
        _footer.Foreground = ConsoleColor.Yellow;
        _footer.Background = ConsoleColor.DarkGray;

        Vt100.SetScrollMargins(2, Console.WindowHeight - 1);

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
        if (_documentService.ViewFiles)
            _view.LineRenderer = FileDocumentLineRenderer.Default;
        else
            _view.LineRenderer = PatchDocumentLineRenderer.Default;

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

        if (_documentService.ViewFiles)
        {
            _header.Text = $" {mode} | Files ";
        }
        else
        {
            var document = (PatchDocument)_documentService.Document;
            var entry = document.Lines.Any() ? document.FindEntry(_view.SelectedLine) : null;
            var emptyMarker = _documentService.ViewStage ? "*nothing to commit*" : "*clean*";
            var path = entry is null ? emptyMarker : entry.Changes.Path;
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

        var tipTree = _gitService.Repository.Head.Tip?.Tree;
        var stageChanges = _gitService.Repository.Diff.Compare<TreeChanges>(tipTree, DiffTargets.Index);
        var stageAdded = stageChanges.Added.Count();
        var stageModified = stageChanges.Modified.Count();
        var stageDeleted = stageChanges.Deleted.Count();

        var workingChanges = _gitService.Repository.Diff.Compare<TreeChanges>(null, true);
        var workingAdded = workingChanges.Added.Count();
        var workingModified = workingChanges.Modified.Count();
        var workingDeleted = workingChanges.Deleted.Count();

        var lineNumberText = _inputLineDigits.Length > 0 ? $"L{_inputLineDigits.ToString()}" : "";

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
            Vt100.EraseRestOfCurrentLine();
            Console.Write(sb);
            Vt100.ShowCursor();

            var k = Console.ReadKey(intercept: true);

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

        var searchResults = new SearchResults(_view.Document, sb.ToString());
        if (searchResults.Hits.Count == 0)
        {
            Vt100.HideCursor();
            Vt100.SetCursorPosition(0, Console.WindowHeight - 1);
            Vt100.SetForegroundColor(ConsoleColor.Blue);
            Vt100.SetBackgroundColor(ConsoleColor.Gray);
            Vt100.EraseRestOfCurrentLine();
            Console.Write("<< NO RESULTS FOUND >>");
            Console.ReadKey();
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

        _view.LineRenderer = ViewLineRenderer.Default;
        _view.Document = new HelpDocument(commands);

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
        Console.Clear();
        Console.WriteLine(ex.Message);
        Console.ReadKey();
    }
}