using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using LibGit2Sharp;

namespace GitIStage
{
    internal sealed class Application
    {
        private readonly string _repositoryPath;
        private readonly string _pathToGit;

        private bool _done;
        private bool _fullFileDiff;
        private int _contextLines = 3;
        private Repository _repository;
        private bool _viewStage;
        private Label _header;
        private View _view;
        private Label _footer;
        private PatchDocument _document;
        private StringBuilder _inputLineDigits = new StringBuilder();
        private bool _helpShowed;
        ConsoleCommand[] _commands;

        public Application(string repositoryPath, string pathToGit)
        {
            _repositoryPath = repositoryPath;
            _pathToGit = pathToGit;
        }

        public void Run()
        {
            _commands = new[]
            {
                new ConsoleCommand(Exit, ConsoleKey.Escape, "Return to command line."),
                new ConsoleCommand(Exit, ConsoleKey.Q, "Return to command line."),
                new ConsoleCommand(Commit, ConsoleKey.C, "Author commit"),
                new ConsoleCommand(CommitAmend, ConsoleKey.C, ConsoleModifiers.Alt, "Authors commit with --amend option"),
                new ConsoleCommand(Stash, ConsoleKey.S, ConsoleModifiers.Alt, "Stashes changes from the working copy, but leaves the stage as-is."),
                new ConsoleCommand(ToggleBetweenWorkingDirectoryAndStaging, ConsoleKey.T, "Toggle between working copy changes and staged changes."),
                new ConsoleCommand(IncreaseContext, ConsoleKey.OemPlus, "Increases the number of contextual lines."),
                new ConsoleCommand(DecreaseContext, ConsoleKey.OemMinus, "Decreases the number of contextual lines."),
                new ConsoleCommand(ToogleFullDiff, ConsoleKey.Oem7, "Toggles between standard diff and full diff"),
                new ConsoleCommand(ToggleWhitespace, ConsoleKey.W, "Toggles between showing and hiding whitespace."),
                new ConsoleCommand(GoHome, ConsoleKey.Home, "Selects the first line."),
                new ConsoleCommand(GoHomeOrInputLine, ConsoleKey.G, "Selects the first line (or Line N)."),
                new ConsoleCommand(GoEnd, ConsoleKey.End, "Selects the last line."),
                new ConsoleCommand(GoEndOrInputLine, ConsoleKey.G, ConsoleModifiers.Shift, "Selects the last line (or Line N)."),
                new ConsoleCommand(SelectUp, ConsoleKey.UpArrow, "Selects the previous line."),
                new ConsoleCommand(SelectUp, ConsoleKey.K, "Selects the previous line."),
                new ConsoleCommand(SelectDown, ConsoleKey.DownArrow, "Selects the next line."),
                new ConsoleCommand(SelectDown, ConsoleKey.J, "Selects the next line."),
                new ConsoleCommand(ScrollUp, ConsoleKey.UpArrow, ConsoleModifiers.Control, "Scrolls up by one line."),
                new ConsoleCommand(ScrollDown, ConsoleKey.DownArrow, ConsoleModifiers.Control, "Scrolls down by one line."),
                new ConsoleCommand(ScrollPageUp, ConsoleKey.PageUp, "Selects the line one screen above."),
                new ConsoleCommand(ScrollPageDown, ConsoleKey.PageDown, "Selects the line one screen below."),
                new ConsoleCommand(ScrollPageDown, ConsoleKey.Spacebar, "Selects the line one screen below."),
                new ConsoleCommand(ScrollLeft, ConsoleKey.LeftArrow, ConsoleModifiers.Control, "Scrolls left by one character."),
                new ConsoleCommand(ScrollRight, ConsoleKey.RightArrow, ConsoleModifiers.Control, "Scrolls right by one character."),
                new ConsoleCommand(GoPreviousFile, ConsoleKey.LeftArrow, "Go to the previous file."),
                new ConsoleCommand(GoNextFile, ConsoleKey.RightArrow, "Go to the next file."),
                new ConsoleCommand(GoPreviousHunk, ConsoleKey.Oem4, "Go to previous change block."),
                new ConsoleCommand(GoNextHunk, ConsoleKey.Oem6, "Go to next change block."),
                new ConsoleCommand(Reset, ConsoleKey.R, "When viewing the working copy, removes the selected line from the working copy."),
                new ConsoleCommand(ResetHunk, ConsoleKey.R, ConsoleModifiers.Shift, "When viewing the working copy, removes the selected block from the working copy."),
                new ConsoleCommand(Stage, ConsoleKey.S, "When viewing the working copy, stages the selected line."),
                new ConsoleCommand(StageHunk, ConsoleKey.S, ConsoleModifiers.Shift, "When viewing the working copy, stages the selected block."),
                new ConsoleCommand(Unstage, ConsoleKey.U, "When viewing the stage, unstages the selected line."),
                new ConsoleCommand(UnstageHunk, ConsoleKey.U, ConsoleModifiers.Shift, "When viewing the stage, unstages the selected block."),
                new ConsoleCommand(RemoveLastLineDigit, ConsoleKey.Backspace, "Remove last line digit"),
                new ConsoleCommand(AppendLineDigit0, ConsoleKey.D0, "Append digit 0 to line."),
                new ConsoleCommand(AppendLineDigit1, ConsoleKey.D1, "Append digit 1 to line."),
                new ConsoleCommand(AppendLineDigit2, ConsoleKey.D2, "Append digit 2 to line."),
                new ConsoleCommand(AppendLineDigit3, ConsoleKey.D3, "Append digit 3 to line."),
                new ConsoleCommand(AppendLineDigit4, ConsoleKey.D4, "Append digit 4 to line."),
                new ConsoleCommand(AppendLineDigit5, ConsoleKey.D5, "Append digit 5 to line."),
                new ConsoleCommand(AppendLineDigit6, ConsoleKey.D6, "Append digit 6 to line."),
                new ConsoleCommand(AppendLineDigit7, ConsoleKey.D7, "Append digit 7 to line."),
                new ConsoleCommand(AppendLineDigit8, ConsoleKey.D8, "Append digit 8 to line."),
                new ConsoleCommand(AppendLineDigit9, ConsoleKey.D9, "Append digit 9 to line."),
                new ConsoleCommand(ShowHelpPage, ConsoleKey.F1, "Show / hide help page")
            };

            Console.CursorVisible = false;
            Console.Clear();

            InitializeScreen();

            using (_repository)
            {
                while (!_done)
                {
                    var width = Console.WindowWidth;
                    var height = Console.WindowHeight;

                    var key = Console.ReadKey(true);
                    var command = _commands.FirstOrDefault(c => c.MatchesKey(key));
                    command?.Execute();

                    if (width != Console.WindowWidth || height != Console.WindowHeight)
                        InitializeScreen();
                }

                Console.Clear();
                Console.CursorVisible = true;
            }
        }

        private void InitializeScreen()
        {
            var oldView = _view;

            _header = new Label(0, 0, Console.WindowWidth);
            _header.Foreground = ConsoleColor.Yellow;
            _header.Background = ConsoleColor.DarkGray;

            var renderer = new PatchDocumentLineRenderer();
            _view = new View(renderer, 1, 0, Console.WindowHeight - 1, Console.WindowWidth);
            _view.SelectedLineChanged += delegate { UpdateHeader(); };

            _footer = new Label(Console.WindowHeight - 1, 0, Console.WindowWidth);
            _footer.Foreground = ConsoleColor.Yellow;
            _footer.Background = ConsoleColor.DarkGray;

            UpdateRepository();

            if (oldView != null)
            {
                _view.VisibleWhitespace = oldView.VisibleWhitespace;
                _view.SelectedLine = oldView.SelectedLine;
                _view.BringIntoView(_view.SelectedLine);   
            }
        }

        private void UpdateRepository()
        {
            _repository?.Dispose();
            _repository = new Repository(_repositoryPath);

            var compareOptions = new CompareOptions();
            compareOptions.ContextLines = _fullFileDiff ? int.MaxValue : _contextLines;

            var tipTree = _repository.Head.Tip?.Tree;
            var changes = _viewStage
                ? _repository.Diff.Compare<TreeChanges>(tipTree, DiffTargets.Index)
                : _repository.Diff.Compare<TreeChanges>(null, true);
            var paths = changes.Select(c => c.Path).ToArray();
            var patch = paths.Any()
                ? _viewStage
                    ? _repository.Diff.Compare<Patch>(tipTree, DiffTargets.Index, paths, null, compareOptions)
                    : _repository.Diff.Compare<Patch>(paths, true, null, compareOptions)
                : null;
           
            _document = PatchDocument.Parse(patch);
            _view.Document = _document;

            UpdateHeader();
            UpdateFooter();
        }

        private void UpdateHeader()
        {
            if (_helpShowed)
            {
                _header.Text = "Keyboard shortcuts";
                return;
            }

            var entry = _document.Lines.Any() ? _document.FindEntry(_view.SelectedLine) : null;
            var emptyMarker = _viewStage ? "*nothing to commit*" : "*clean*";
            var path = entry == null ? emptyMarker : entry.Changes.Path;
            var mode = _viewStage ? "S" : "W";

            _header.Text = $" {mode} | {path}";
        }

        private void UpdateFooter()
        {
            if (_helpShowed)
            {
                _footer.Text = string.Empty;
                return;
            }

            var tipTree = _repository.Head.Tip?.Tree;
            var stageChanges = _repository.Diff.Compare<TreeChanges>(tipTree, DiffTargets.Index);
            var stageAdded = stageChanges.Added.Count();
            var stageModified = stageChanges.Modified.Count();
            var stageDeleted = stageChanges.Deleted.Count();

            var workingChanges = _repository.Diff.Compare<TreeChanges>(null, true);
            var workingAdded = workingChanges.Added.Count();
            var workingModified = workingChanges.Modified.Count();
            var workingDeleted = workingChanges.Deleted.Count();

            var lineNumberText = _inputLineDigits.Length > 0 ? $"L{_inputLineDigits.ToString()}" : "";
            
            _footer.Text = $" [{_repository.Head.FriendlyName} +{stageAdded} ~{stageModified} -{stageDeleted} | +{workingAdded} ~{workingModified} -{workingDeleted}]    {lineNumberText} ";
        }

        private void Exit()
        {
            _done = true;
        }

        private void RunGit(string command)
        {
            var startupInfo = new ProcessStartInfo
            {
                FileName = _pathToGit,
                Arguments = command,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            var process = Process.Start(startupInfo);
            process.WaitForExit();

            UpdateRepository();
        }

        private void Commit()
        {
            RunGit("commit -v");
        }

        private void CommitAmend()
        {
            RunGit("commit -v --amend");
        }

        private void Stash()
        {
            RunGit("stash -u -k");
        }

        private void ToggleBetweenWorkingDirectoryAndStaging()
        {
            _viewStage = !_viewStage;

            UpdateRepository();
        }

        private void IncreaseContext()
        {
            _contextLines++;
            UpdateRepository();
        }

        private void DecreaseContext()
        {
            if (_contextLines == 0)
                return;

            _contextLines--;
            UpdateRepository();
        }

        private void ToogleFullDiff()
        {
            _fullFileDiff = !_fullFileDiff;
            UpdateRepository();
        }

        private void ToggleWhitespace()
        {
            _view.VisibleWhitespace = !_view.VisibleWhitespace;
        }

        private void GoHome()
        {
            _view.LeftChar = 0;
            _view.SelectedLine = 0;
            _view.BringIntoView(_view.SelectedLine);
        }

        private void GoHomeOrInputLine()
        {
            if (_inputLineDigits.Length == 0)
                GoHome();
            else
                GoInputLine();
        }

        private void GoEnd()
        {
            _view.LeftChar = 0;
            _view.SelectedLine = _view.DocumentHeight - 1;
            _view.BringIntoView(_view.SelectedLine);
        }

        private void GoEndOrInputLine()
        {
            if (_inputLineDigits.Length == 0)
                GoEnd();
            else
                GoInputLine();
        }

        private void GoInputLine()
        {
            if (Int32.TryParse(_inputLineDigits.ToString(), out int line))
            {
                GoLine(line);
                _inputLineDigits.Clear();
                UpdateFooter();
            }
        }

        private void GoLine(int line)
        {
            line = line - 1;
            if (line < 0)
                line = 0;
            else if (line >= _document.Height)
                line = _document.Height - 1;

            _view.LeftChar = 0;
            _view.SelectedLine = line;
            _view.BringIntoView(_view.SelectedLine);
        }

        private void SelectUp()
        {
            if (_view.SelectedLine <= 0)
                return;

            _view.SelectedLine--;
            if (_view.TopLine > _view.SelectedLine)
                _view.TopLine = _view.SelectedLine;
        }

        private void SelectDown()
        {
            if (_view.SelectedLine == _view.DocumentHeight - 1)
                return;

            _view.SelectedLine++;
            if (_view.TopLine < _view.SelectedLine - _view.Height + 1)
                _view.TopLine = _view.SelectedLine - _view.Height + 1;
        }

        private void ScrollUp()
        {
            if (_view.TopLine == 0)
                return;

            _view.TopLine--;
        }

        private void ScrollDown()
        {
            if (_view.TopLine >= _view.DocumentHeight - _view.Height)
                return;

            _view.TopLine++;
        }

        private void ScrollPageUp()
        {
            _view.TopLine = Math.Max(0, _view.TopLine - _view.Height);
            _view.SelectedLine = _view.TopLine;
        }

        private void ScrollPageDown()
        {
            _view.TopLine = Math.Min(_view.DocumentHeight - _view.Height, _view.TopLine + _view.Height);
            _view.SelectedLine = _view.TopLine;
        }

        private void ScrollLeft()
        {
            if (_view.LeftChar == 0)
                return;

            _view.LeftChar--;
        }

        private void ScrollRight()
        {
            if (_view.LeftChar == _view.DocumentWidth - _view.Width)
                return;

            _view.LeftChar++;
        }

        private void GoPreviousFile()
        {
            var i = _view.SelectedLine;
            if (i < 0)
                return;

            var nextIndex = _document.FindPreviousEntryIndex(i);
            _view.SelectedLine = _document.Entries[nextIndex].Offset;
            _view.BringIntoView(_view.SelectedLine);
        }

        private void GoNextFile()
        {
            var i = _view.SelectedLine;
            if (i < 0)
                return;

            var nextIndex = _document.FindNextEntryIndex(i);
            _view.SelectedLine = _document.Entries[nextIndex].Offset;
            _view.BringIntoView(_view.SelectedLine);
        }

        private void GoPreviousHunk()
        {
            var i = _view.SelectedLine;
            if (i < 0)
                return;

            _view.SelectedLine = _document.FindPreviousChangeBlock(i);
            _view.BringIntoView(_view.SelectedLine);
        }

        private void GoNextHunk()
        {
            var i = _view.SelectedLine;
            if (i < 0)
                return;

            _view.SelectedLine = _document.FindNextChangeBlock(i);
            _view.BringIntoView(_view.SelectedLine);
        }

        private void Reset()
        {
            if (_viewStage)
                return;

            ApplyPatch(PatchDirection.Reset, false);
        }

        private void ResetHunk()
        {
            if (_viewStage)
                return;

            ApplyPatch(PatchDirection.Reset, true);
        }

        private void Stage()
        {
            if (_viewStage)
                return;

            ApplyPatch(PatchDirection.Stage, false);
        }

        private void StageHunk()
        {
            if (_viewStage)
                return;

            ApplyPatch(PatchDirection.Stage, true);
        }

        private void Unstage()
        {
            if (!_viewStage)
                return;

            ApplyPatch(PatchDirection.Unstage, false);
        }

        private void UnstageHunk()
        {
            if (!_viewStage)
                return;

            ApplyPatch(PatchDirection.Unstage, true);
        }

        private void RemoveLastLineDigit()
        {
            if (_inputLineDigits.Length == 0)
                return;

            _inputLineDigits.Remove(_inputLineDigits.Length - 1, 1);
            UpdateFooter();
        }

        private void AppendLineDigit0() => AppendLineDigit('0');
        private void AppendLineDigit1() => AppendLineDigit('1');
        private void AppendLineDigit2() => AppendLineDigit('2');
        private void AppendLineDigit3() => AppendLineDigit('3');
        private void AppendLineDigit4() => AppendLineDigit('4');
        private void AppendLineDigit5() => AppendLineDigit('5');
        private void AppendLineDigit6() => AppendLineDigit('6');
        private void AppendLineDigit7() => AppendLineDigit('7');
        private void AppendLineDigit8() => AppendLineDigit('8');
        private void AppendLineDigit9() => AppendLineDigit('9');

        private void AppendLineDigit(char digit)
        {
            _inputLineDigits.Append(digit);
            UpdateFooter();
        }

        private void ShowHelpPage()
        {
            if (_helpShowed)
            {
                _helpShowed = false;
                UpdateRepository();
                return;
            }

            var options = new ConsoleTableOptions { EnableCount = false, Columns = new List<string> { "Shortcut", "Description" }, HideRowLines = true };
            var table = new ConsoleTable(options);

            foreach (var command in _commands)
            {
                table.AddRow(command.GetCommandShortcut(), command.Description);
            }

            //foreach (var line in new Shortcuts().Get())
            //{
            //    string[] split = line.Text.Split("|");
            //    table.AddRow(split[0].Trim(), split[1].Trim());
            //}

            string[] lines = table.ToString().Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var patchLines = new List<PatchLine>(lines.Length);
            foreach (var line in lines)
            {
                patchLines.Add(new PatchLine(PatchLineKind.Context, line));
            }

            _view.Document = new PatchDocument(null, patchLines);
            _helpShowed = true;

            UpdateHeader();
            UpdateFooter();
        }

        private void ApplyPatch(PatchDirection direction, bool entireHunk)
        {
            if (_view.SelectedLine < 0)
                return;

            var line = _document.Lines[_view.SelectedLine];
            if (!line.Kind.IsAdditionOrRemoval())
                return;

            IEnumerable<int> lines;
            if (!entireHunk)
            {
                lines = new[] {_view.SelectedLine};
            }
            else
            {
                var start = _document.FindStartOfChangeBlock(_view.SelectedLine);
                var end = _document.FindEndOfChangeBlock(_view.SelectedLine);
                var length = end - start + 1;
                lines = Enumerable.Range(start, length);
            }
            var patch = Patching.ComputePatch(_document, lines, direction);

            Patching.ApplyPatch(_pathToGit, _repository.Info.WorkingDirectory, patch, direction);
            UpdateRepository();
        }
    }
}