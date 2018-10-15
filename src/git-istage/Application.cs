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

        public Application(string repositoryPath, string pathToGit)
        {
            _repositoryPath = repositoryPath;
            _pathToGit = pathToGit;
        }

        public void Run()
        {
            var commands = new[]
            {
                new ConsoleCommand(Exit, ConsoleKey.Escape),
                new ConsoleCommand(Exit, ConsoleKey.Q),
                new ConsoleCommand(Commit, ConsoleKey.C),
                new ConsoleCommand(CommitAmend, ConsoleKey.C, ConsoleModifiers.Alt),
                new ConsoleCommand(Stash, ConsoleKey.S, ConsoleModifiers.Alt),
                new ConsoleCommand(ToggleBetweenWorkingDirectoryAndStaging, ConsoleKey.T),
                new ConsoleCommand(IncreaseContext, ConsoleKey.OemPlus),
                new ConsoleCommand(DecreaseContext, ConsoleKey.OemMinus),
                new ConsoleCommand(ToogleFullDiff, ConsoleKey.Oem7),
                new ConsoleCommand(ToggleWhitespace, ConsoleKey.W),
                new ConsoleCommand(GoHome, ConsoleKey.Home),
                new ConsoleCommand(GoHomeOrInputLine, ConsoleKey.G),
                new ConsoleCommand(GoEnd, ConsoleKey.End),
                new ConsoleCommand(GoEndOrInputLine, ConsoleKey.G, ConsoleModifiers.Shift),
                new ConsoleCommand(SelectUp, ConsoleKey.UpArrow),
                new ConsoleCommand(SelectUp, ConsoleKey.K),
                new ConsoleCommand(SelectDown, ConsoleKey.DownArrow),
                new ConsoleCommand(SelectDown, ConsoleKey.J),
                new ConsoleCommand(ScrollUp, ConsoleKey.UpArrow, ConsoleModifiers.Control),
                new ConsoleCommand(ScrollDown, ConsoleKey.DownArrow, ConsoleModifiers.Control),
                new ConsoleCommand(ScrollPageUp, ConsoleKey.PageUp),
                new ConsoleCommand(ScrollPageDown, ConsoleKey.PageDown),
                new ConsoleCommand(ScrollPageDown, ConsoleKey.Spacebar),
                new ConsoleCommand(ScrollLeft, ConsoleKey.LeftArrow, ConsoleModifiers.Control),
                new ConsoleCommand(ScrollRight, ConsoleKey.RightArrow, ConsoleModifiers.Control),
                new ConsoleCommand(GoPreviousFile, ConsoleKey.LeftArrow),
                new ConsoleCommand(GoNextFile, ConsoleKey.RightArrow),
                new ConsoleCommand(GoPreviousHunk, ConsoleKey.Oem4),
                new ConsoleCommand(GoNextHunk, ConsoleKey.Oem6),
                new ConsoleCommand(Reset, ConsoleKey.R),
                new ConsoleCommand(ResetHunk, ConsoleKey.R, ConsoleModifiers.Shift),
                new ConsoleCommand(Stage, ConsoleKey.S),
                new ConsoleCommand(StageHunk, ConsoleKey.S, ConsoleModifiers.Shift),
                new ConsoleCommand(Unstage, ConsoleKey.U),
                new ConsoleCommand(UnstageHunk, ConsoleKey.U, ConsoleModifiers.Shift),
                new ConsoleCommand(RemoveLastLineDigit, ConsoleKey.Backspace),
                new ConsoleCommand(AppendLineDigit0, ConsoleKey.D0),
                new ConsoleCommand(AppendLineDigit1, ConsoleKey.D1),
                new ConsoleCommand(AppendLineDigit2, ConsoleKey.D2),
                new ConsoleCommand(AppendLineDigit3, ConsoleKey.D3),
                new ConsoleCommand(AppendLineDigit4, ConsoleKey.D4),
                new ConsoleCommand(AppendLineDigit5, ConsoleKey.D5),
                new ConsoleCommand(AppendLineDigit6, ConsoleKey.D6),
                new ConsoleCommand(AppendLineDigit7, ConsoleKey.D7),
                new ConsoleCommand(AppendLineDigit8, ConsoleKey.D8),
                new ConsoleCommand(AppendLineDigit9, ConsoleKey.D9),
                new ConsoleCommand(ShowHelpPage, ConsoleKey.F1)
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
                    var command = commands.FirstOrDefault(c => c.MatchesKey(key));
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
            var entry = _document.Lines.Any() ? _document.FindEntry(_view.SelectedLine) : null;
            var emptyMarker = _viewStage ? "*nothing to commit*" : "*clean*";
            var path = entry == null ? emptyMarker : entry.Changes.Path;
            var mode = _viewStage ? "S" : "W";

            _header.Text = $" {mode} | {path}";
        }

        private void UpdateFooter()
        {
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
            //if (!_viewStage)
            //    return;

            _view.Document = new PatchDocument(null, new Shortcuts().Get());
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