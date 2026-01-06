using System.Reflection;
using GitIStage.Commands;
using GitIStage.Patches;
using GitIStage.UI;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;

namespace GitIStage.Services;

// TODO: Ideally, we should be able to test this class as there is a ton of policy.
//       However, in order to do that, we need to be able to abstract the UI service.
internal sealed class CommandService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly GitService _gitService;
    private readonly DocumentService _documentService;
    private readonly PatchingService _patchingService;
    private readonly UIService _uiService;
    private readonly IReadOnlyList<ConsoleCommand> _commands;

    public CommandService(IServiceProvider serviceProvider,
                          KeyBindingService keyBindingService,
                          GitService gitService,
                          DocumentService documentService,
                          PatchingService patchingService,
                          UIService uiService)
    {
        _serviceProvider = serviceProvider;
        _gitService = gitService;
        _documentService = documentService;
        _patchingService = patchingService;
        _uiService = uiService;

        var commands = CreateCommands();
        _commands = BindUserKeys(commands, keyBindingService);
    }

    public IReadOnlyList<ConsoleCommand> Commands => _commands;

    public ConsoleCommand? GetCommand(ConsoleKeyInfo keyInfo)
    {
        return _commands.FirstOrDefault(c => c.KeyBindings.Any(b => b.Matches(keyInfo)));
    }

    private IReadOnlyList<ConsoleCommand> CreateCommands()
    {
        var handlers = GetType().GetMethods(BindingFlags.Public |
                                            BindingFlags.NonPublic |
                                            BindingFlags.Instance |
                                            BindingFlags.Static)
                                .Where(m => m.GetParameters().Length == 0)
                                .Select(m => (Method: m, Attribute: m.GetCustomAttributes<CommandHandlerAttribute>().FirstOrDefault()))
                                .Where(t => t.Attribute is not null)
                                .Select(t => (t.Method, Attribute: t.Attribute!))
                                .ToArray();

        var commands = new List<ConsoleCommand>(handlers.Length);

        foreach (var (method, attribute) in handlers)
        {
            var instance = method.IsStatic ? null : this;
            var action = method.CreateDelegate<Action>(instance);
            var keyBindings = attribute.GetKeyBindings();
            var description = attribute.Description;
            var command = new ConsoleCommand(method.Name, action, keyBindings, description);
            commands.Add(command);
        }

        return commands.ToArray();
    }

    private static ConsoleCommand[] BindUserKeys(IEnumerable<ConsoleCommand> commands, KeyBindingService keyBindingService)
    {
        var commandByName = commands.ToDictionary(c => c.Name);
        var userKeyBindings = keyBindingService.GetUserKeyBindings();

        foreach (var (name, bindings) in userKeyBindings)
        {
            if (!commandByName.TryGetValue(name, out var command))
                throw ExceptionBuilder.KeyBindingsReferToInvalidCommand(keyBindingService.GetUserKeyBindingsPath(), name);

            commandByName[name] = command.WithKeyBindings(bindings);
        }

        return commandByName.Values.OrderBy(c => c.Name).ToArray();
    }

    [CommandHandler("Return to command line.", "Esc", "Q")]
    private void Exit()
    {
        var application = _serviceProvider.GetRequiredService<Application>();
        application.Exit();
    }

    [CommandHandler("Author commit", "C")]
    private void Commit()
    {
        ExecuteCommit(amend: false);
    }

    [CommandHandler("Amend commit", "Alt+C")]
    private void CommitAmend()
    {
        ExecuteCommit(amend: true);
    }

    private void ExecuteCommit(bool amend)
    {
        if (_uiService.HelpShowing)
            return;

        var tipTree = _gitService.Repository.Head.Tip?.Tree;
        if (!_gitService.Repository.Diff.Compare<TreeChanges>(tipTree, DiffTargets.Index).Any())
            return;

        using (_gitService.SuspendEvents())
        {
            _uiService.Hide();
            _gitService.Commit(amend);
            _uiService.Show();
        }
    }

    [CommandHandler("Stashes changes from the working copy, but leaves the stage as-is.", "Alt+S")]
    private void Stash()
    {
        if (_uiService.HelpShowing) return;
        _gitService.StashUntrackedKeepIndex();
    }

    [CommandHandler("Toggle between working copy changes and staged changes.", "T")]
    private void ToggleBetweenWorkingDirectoryAndStaging()
    {
        if (_uiService.HelpShowing) return;
        _documentService.ViewStage = !_documentService.ViewStage;
    }

    [CommandHandler("Toggle between seeing changes and changed files.", "F")]
    private void ToggleFilesAndChanges()
    {
        if (_uiService.HelpShowing) return;
        _documentService.ViewFiles = !_documentService.ViewFiles;
    }

    [CommandHandler("Increases the number of contextual lines.", "OemPlus")]
    private void IncreaseContext()
    {
        if (_uiService.HelpShowing) return;
        _documentService.ContextLines += 1;
    }

    [CommandHandler("Decreases the number of contextual lines.", "OemMinus")]
    private void DecreaseContext()
    {
        if (_uiService.HelpShowing) return;
        if (_documentService.ContextLines == 0)
            return;

        _documentService.ContextLines -= 1;
    }

    [CommandHandler("Toggles between standard diff and full diff", "Oem7")]
    private void ToggleFullDiff()
    {
        if (_uiService.HelpShowing) return;
        _documentService.ViewFullDiff = !_documentService.ViewFullDiff;
    }

    [CommandHandler("Toggles between showing and hiding whitespace.", "W")]
    private void ToggleWhitespace()
    {
        if (_uiService.HelpShowing) return;
        _uiService.View.VisibleWhitespace = !_uiService.View.VisibleWhitespace;
    }

    [CommandHandler("Selects the first line.", "Home")]
    private void GoHome()
    {
        if (_uiService.View.DocumentHeight == 0)
            return;

        _uiService.View.LeftChar = 0;
        _uiService.View.SelectedLine = 0;
        _uiService.View.BringIntoView(_uiService.View.SelectedLine);
    }

    [CommandHandler("Selects the first line (or Line N).", "G")]
    private void GoHomeOrInputLine()
    {
        if (_uiService.HasInputLine)
            GoInputLine();
        else
            GoHome();
    }

    [CommandHandler("Selects the last line.", "End")]
    private void GoEnd()
    {
        if (_uiService.View.DocumentHeight == 0)
            return;

        _uiService.View.LeftChar = 0;
        _uiService.View.SelectedLine = _uiService.View.DocumentHeight - 1;
        _uiService.View.BringIntoView(_uiService.View.SelectedLine);
    }

    [CommandHandler("Selects the last line (or Line N).", "Shift+G")]
    private void GoEndOrInputLine()
    {
        if (_uiService.HasInputLine)
            GoInputLine();
        else
            GoEnd();
    }

    private void GoInputLine()
    {
        if (_uiService.TryGetInputLine(out var line))
            GoLine(line);
    }

    private void GoLine(int line)
    {
        if (_uiService.View.DocumentHeight == 0)
            return;

        line -= 1;
        if (line < 0)
            line = 0;
        else if (line >= _documentService.Document.Height)
            line = _documentService.Document.Height - 1;

        _uiService.View.LeftChar = 0;
        _uiService.View.SelectedLine = line;
        _uiService.View.BringIntoView(_uiService.View.SelectedLine);
    }

    [CommandHandler("Selects the previous line.", "UpArrow", "K")]
    private void SelectUp()
    {
        if (_uiService.View.SelectedLine <= 0)
            return;

        _uiService.View.SelectedLine--;
    }

    [CommandHandler("Selects the next line.", "DownArrow", "J")]
    private void SelectDown()
    {
        if (_uiService.View.SelectedLine == _uiService.View.DocumentHeight - 1)
            return;

        _uiService.View.SelectedLine++;
    }
    
    [CommandHandler("Extends the selection to the previous line.", "Shift+UpArrow", "Shift+K")]
    private void ExtendSelectionUp()
    {
        var selection = _uiService.View.Selection;

        if (selection.AtEnd)
        {
            // Shrink end
            if (selection.Count > 0)
                _uiService.View.Selection = new Selection(selection.StartLine, selection.Count - 1, true);
            else if (selection.StartLine > 0)
                _uiService.View.Selection = new Selection(selection.StartLine - 1, 1);
        }
        else
        {
            // Extend start
            if (selection.StartLine > 0)
                _uiService.View.Selection = new Selection(selection.StartLine - 1, selection.Count + 1);
        }
    }

    [CommandHandler("Extends the selection to the next line.", "Shift+DownArrow", "Shift+J")]
    private void ExtendSelectionDown()
    {
        var selection = _uiService.View.Selection; 

        if (selection.AtEnd)
        {
            // Extend end
            if (selection.StartLine < _uiService.View.DocumentHeight - 1)
                _uiService.View.Selection = new Selection(selection.StartLine, selection.Count + 1, true);
        }
        else
        {
            // Shrink start
            if (selection.Count > 0)
                _uiService.View.Selection = new Selection(selection.StartLine + 1, selection.Count - 1);
            else if (selection.EndLine < _uiService.View.DocumentHeight - 1)
                _uiService.View.Selection = new Selection(selection.StartLine, selection.Count + 1, true);
        }
    }

    [CommandHandler("Scrolls up by one line.", "Control+UpArrow")]
    private void ScrollUp()
    {
        if (_uiService.View.TopLine == 0)
            return;

        _uiService.View.TopLine--;
    }

    [CommandHandler("Scrolls down by one line.", "Control+DownArrow")]
    private void ScrollDown()
    {
        if (_uiService.View.TopLine >= _uiService.View.DocumentHeight - _uiService.View.Height)
            return;

        _uiService.View.TopLine++;
    }

    [CommandHandler("Selects the line one screen above.", "PageUp")]
    private void ScrollPageUp()
    {
        var delta = Math.Min(_uiService.View.Height, _uiService.View.SelectedLine);
        _uiService.View.TopLine = Math.Max(0, _uiService.View.TopLine - delta);
        _uiService.View.SelectedLine = _uiService.View.SelectedLine - delta;
    }

    [CommandHandler("Selects the line one screen below.", "PageDown", "SpaceBar")]
    private void ScrollPageDown()
    {
        var delta = Math.Min(_uiService.View.Height, _uiService.View.DocumentHeight - _uiService.View.SelectedLine - 1);
        _uiService.View.TopLine = Math.Min(
            Math.Max(0, _uiService.View.DocumentHeight - _uiService.View.Height),
            _uiService.View.TopLine + delta);
        _uiService.View.SelectedLine = _uiService.View.SelectedLine + delta;
    }

    [CommandHandler("Scrolls left by one character.", "Control+LeftArrow")]
    private void ScrollLeft()
    {
        if (_uiService.View.LeftChar == 0)
            return;

        _uiService.View.LeftChar--;
    }

    [CommandHandler("Scrolls right by one character.", "Control+RightArrow")]
    private void ScrollRight()
    {
        if (_uiService.View.LeftChar == _uiService.View.DocumentWidth - _uiService.View.Width)
            return;

        _uiService.View.LeftChar++;
    }

    [CommandHandler("Go to the previous file.", "LeftArrow")]
    private void GoPreviousFile()
    {
        if (_uiService.HelpShowing) return;
        var i = _uiService.View.SelectedLine;
        if (i < 0)
            return;

        var document = _documentService.Document;
        var nextIndex = document.FindPreviousEntryIndex(i);
        if (nextIndex >= 0)
        {
            _uiService.View.SelectedLine = document.GetLineIndex(nextIndex);
            _uiService.View.BringIntoView(_uiService.View.SelectedLine);
        }
    }

    [CommandHandler("Go to the next file.", "RightArrow")]
    private void GoNextFile()
    {
        if (_uiService.HelpShowing) return;
        var i = _uiService.View.SelectedLine;
        if (i < 0)
            return;

        var document = _documentService.Document;
        var nextIndex = document.FindNextEntryIndex(i);
        if (nextIndex >= 0)
        {
            _uiService.View.SelectedLine = document.GetLineIndex(nextIndex);
            _uiService.View.BringIntoView(_uiService.View.SelectedLine);
        }
    }

    [CommandHandler("Go to previous change block.", "Oem4")]
    private void GoPreviousHunk()
    {
        if (_uiService.HelpShowing) return;
        var i = _uiService.View.SelectedLine;
        if (i < 0)
            return;

        _uiService.View.SelectedLine = _documentService.Document.FindPreviousChangeBlock(i);
        _uiService.View.BringIntoView(_uiService.View.SelectedLine);
    }

    [CommandHandler("Go to next change block.", "Oem6")]
    private void GoNextHunk()
    {
        if (_uiService.HelpShowing) return;
        var i = _uiService.View.SelectedLine;
        if (i < 0)
            return;

        _uiService.View.SelectedLine = _documentService.Document.FindNextChangeBlock(i);
        _uiService.View.BringIntoView(_uiService.View.SelectedLine);
    }

    [CommandHandler("Searches for a pattern.", "Oem2")]
    private void Search()
    {
        _uiService.Search();
    }

    [CommandHandler("Go to the previous search hit.", "p")]
    private void GoPreviousHit()
    {
        if (_uiService.View.SearchResults is null)
            return;

        var hit = _uiService.View.SearchResults.FindPrevious(_uiService.View.SelectedLine);
        if (hit is not null)
            _uiService.View.SelectedLine = hit.LineIndex;
    }

    [CommandHandler("Go to next search hit.", "n")]
    private void GoNextHit()
    {
        if (_uiService.View.SearchResults is null)
            return;

        var hit = _uiService.View.SearchResults.FindNext(_uiService.View.SelectedLine);
        if (hit is not null)
            _uiService.View.SelectedLine = hit.LineIndex;
    }

    [CommandHandler("When viewing the working copy, removes the selected line from the working copy.", "R")]
    private void Reset()
    {
        if (_uiService.HelpShowing || _documentService.ViewStage)
            return;

        ApplyPatch(PatchDirection.Reset, false);
    }

    [CommandHandler("When viewing the working copy, removes the selected block from the working copy.", "Shift+R")]
    private void ResetHunk()
    {
        if (_uiService.HelpShowing || _documentService.ViewStage)
            return;

        ApplyPatch(PatchDirection.Reset, true);
    }

    [CommandHandler("When viewing the working copy, stages the selected line.", "S")]
    private void Stage()
    {
        if (_uiService.HelpShowing || _documentService.ViewStage)
            return;

        ApplyPatch(PatchDirection.Stage, false);
    }

    [CommandHandler("When viewing the working copy, stages the selected block.", "Shift+S")]
    private void StageHunk()
    {
        if (_uiService.HelpShowing || _documentService.ViewStage)
            return;

        ApplyPatch(PatchDirection.Stage, true);
    }

    [CommandHandler("When viewing the stage, unstages the selected line.", "U")]
    private void Unstage()
    {
        if (_uiService.HelpShowing || !_documentService.ViewStage)
            return;

        ApplyPatch(PatchDirection.Unstage, false);
    }

    [CommandHandler("When viewing the stage, unstages the selected block.", "Shift+U")]
    private void UnstageHunk()
    {
        if (_uiService.HelpShowing || !_documentService.ViewStage)
            return;

        ApplyPatch(PatchDirection.Unstage, true);
    }

    [CommandHandler("Remove last line digit", "BackSpace")]
    private void RemoveLastLineDigit()
    {
        _uiService.RemoveLastLineDigit();
    }

    [CommandHandler("Append digit 0 to line.", "0")]
    public void AppendLineDigit0() => AppendLineDigit('0');

    [CommandHandler("Append digit 1 to line.", "1")]
    private void AppendLineDigit1() => AppendLineDigit('1');

    [CommandHandler("Append digit 2 to line.", "2")]
    private void AppendLineDigit2() => AppendLineDigit('2');

    [CommandHandler("Append digit 3 to line.", "3")]
    private void AppendLineDigit3() => AppendLineDigit('3');

    [CommandHandler("Append digit 4 to line.", "4")]
    private void AppendLineDigit4() => AppendLineDigit('4');

    [CommandHandler("Append digit 5 to line.", "5")]
    private void AppendLineDigit5() => AppendLineDigit('5');

    [CommandHandler("Append digit 6 to line.", "6")]
    private void AppendLineDigit6() => AppendLineDigit('6');

    [CommandHandler("Append digit 7 to line.", "7")]
    private void AppendLineDigit7() => AppendLineDigit('7');

    [CommandHandler("Append digit 8 to line.", "8")]
    private void AppendLineDigit8() => AppendLineDigit('8');

    [CommandHandler("Append digit 9 to line.", "9")]
    private void AppendLineDigit9() => AppendLineDigit('9');

    private void AppendLineDigit(char digit)
    {
        _uiService.AppendLineDigit(digit);
    }

    [CommandHandler("Show / hide help page.", "F1", "Shift+Oem2")]
    private void ShowHelpPage()
    {
        _uiService.HelpShowing = !_uiService.HelpShowing;
    }

    private void ApplyPatch(PatchDirection direction, bool entireHunk)
    {
        if (_uiService.HelpShowing)
            return;

        if (direction == PatchDirection.Stage && _documentService.ViewStage)
            return;

        if (direction == PatchDirection.Unstage && !_documentService.ViewStage)
            return;

        if (direction == PatchDirection.Reset && _documentService.ViewStage)
            return;

        var selection = _uiService.View.Selection;
        if (selection.StartLine < 0)
            return;

        try
        {
            _patchingService.ApplyPatch(direction, entireHunk, selection.StartLine, selection.Count);
        }
        catch (GitCommandFailedException ex)
        {
            _uiService.RenderGitError(ex);
        }
    }
}