using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using LibGit2Sharp;

namespace GitIStage
{
    internal sealed class Application
    {
        private readonly string _repositoryPath;
        private readonly string _pathToGit;
        private readonly KeyBindings _keyBindings;

        private bool _done;
        private bool _fullFileDiff;
        private int _contextLines = 3;
        private Repository _repository;
        private bool _viewFiles;
        private bool _viewStage;
        private Label _header;
        private View _view;
        private Label _footer;
        private FileDocument _fileDocument;
        private PatchDocument _document;
        private StringBuilder _inputLineDigits = new StringBuilder();
        private bool _helpShowing;
        ConsoleCommand[] _commands;

        private int _selectedLineBeforeHelpWasShown;
        private int _topLineBeforeHelpWasShown;

        public Application(string repositoryPath, string pathToGit, KeyBindings keyBindings)
        {
            _repositoryPath = repositoryPath;
            _pathToGit = pathToGit;
            _keyBindings = keyBindings;
        }

        public void Run()
        {
            _commands = LoadConsoleCommands();

            Vt100.SwitchToAlternateBuffer();
            Vt100.HideCursor();

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

                Vt100.ResetScrollMargins();
                Vt100.SwitchToMainBuffer();
                Vt100.ShowCursor();
            }
        }

        private ConsoleCommand[] LoadConsoleCommands()
        {
            var commands = new List<ConsoleCommand>();

            foreach (var handlerName in _keyBindings.Handlers.Keys)
            {
                var binding = _keyBindings.Handlers[handlerName];
                var handler = ResolveHandler(handlerName);
                if (handler == null)
                {
                    Console.WriteLine($"fatal: invalid key binding handler named `{handlerName}`.");
                    Environment.Exit(1);
                }

                foreach (var keyPress in binding.Default)
                {
                    var (key, modifiers) = ResolveKey(keyPress);

                    var command = new ConsoleCommand(
                        handler,
                        key,
                        modifiers,
                        binding.Description
                        );

                    commands.Add(command);
                }
            }

            return commands.ToArray();
        }

        private (ConsoleKey, ConsoleModifiers) ResolveKey(string key)
        {
            var parser = new KeyPressParser();
            var result = parser.Parse(key);
            if (result.Succeeded)
                return (result.Key, result.Modifiers);

            Console.WriteLine($"fatal: invalid key binding '{key}'.");
            Environment.Exit(1);
            return (0, 0);
        }

        private Action ResolveHandler(string handlerName)
        {
            var methodInfo = GetType().GetMethod(handlerName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo == null)
                return null;

            var handler = (Action)Delegate.CreateDelegate(typeof(Action), this, methodInfo);

            return handler;
        }

        private void InitializeScreen()
        {
            var oldView = _view;

            _header = new Label(0, 0, Console.WindowWidth);
            _header.Foreground = ConsoleColor.Yellow;
            _header.Background = ConsoleColor.DarkGray;

            _view = new View(1, 0, Console.WindowHeight - 1, Console.WindowWidth);
            _view.SelectedLineChanged += delegate { UpdateHeader(); };

            _footer = new Label(Console.WindowHeight - 1, 0, Console.WindowWidth);
            _footer.Foreground = ConsoleColor.Yellow;
            _footer.Background = ConsoleColor.DarkGray;

            Vt100.SetScrollMargins(2, Console.WindowHeight - 1);

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

            var paths = changes
                .Where(p => (p.Mode != Mode.SymbolicLink && p.OldMode != Mode.SymbolicLink))
                .Select(c => c.Path).ToArray();

            var patch = paths.Any()
                ? _viewStage
                    ? _repository.Diff.Compare<Patch>(tipTree, DiffTargets.Index, paths, null, compareOptions)
                    : _repository.Diff.Compare<Patch>(paths, true, null, compareOptions)
                : null;

            _fileDocument = FileDocument.Create(_repositoryPath, changes, _viewStage);
            _document = PatchDocument.Parse(patch);

            if (_viewFiles)
            {
                _view.LineRenderer = FileDocumentLineRenderer.Default;
                _view.Document = _fileDocument;
            }
            else
            {
                _view.LineRenderer = PatchDocumentLineRenderer.Default;
                _view.Document = _document;
            }

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

            var entry = _document.Lines.Any() ? _document.FindEntry(_view.SelectedLine) : null;
            var emptyMarker = _viewStage ? "*nothing to commit*" : "*clean*";
            var path = entry == null ? emptyMarker : entry.Changes.Path;
            var mode = _viewStage ? "S" : "W";

            if (_viewFiles)
            {
                _header.Text = $" {mode} | Files ";
            }
            else
            {
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
                UseShellExecute = false,
                WorkingDirectory = _repository.Info.WorkingDirectory
            };

            var process = Process.Start(startupInfo);
            process.WaitForExit();

            UpdateRepository();
        }

        private void Commit()
        {
            if (_helpShowing) return;
            RunGit("commit -v");
        }

        private void CommitAmend()
        {
            if (_helpShowing) return;
            RunGit("commit -v --amend");
        }

        private void Stash()
        {
            if (_helpShowing) return;
            RunGit("stash -u -k");
        }

        private void ToggleBetweenWorkingDirectoryAndStaging()
        {
            if (_helpShowing) return;
            _viewStage = !_viewStage;

            UpdateRepository();
        }

        private void ToggleFilesAndChanges()
        {
            if (_helpShowing) return;
            _viewFiles = !_viewFiles;

            UpdateRepository();
        }

        private void IncreaseContext()
        {
            if (_helpShowing) return;
            _contextLines++;
            UpdateRepository();
        }

        private void DecreaseContext()
        {
            if (_helpShowing) return;
            if (_contextLines == 0)
                return;

            _contextLines--;
            UpdateRepository();
        }

        private void ToggleFullDiff()
        {
            if (_helpShowing) return;
            _fullFileDiff = !_fullFileDiff;
            UpdateRepository();
        }

        private void ToggleWhitespace()
        {
            if (_helpShowing) return;
            _view.VisibleWhitespace = !_view.VisibleWhitespace;
        }

        private void GoHome()
        {
            if (_view.DocumentHeight == 0)
                return;

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
            if (_view.DocumentHeight == 0)
                return;

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
            if (_view.DocumentHeight == 0)
                return;

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
        }

        private void SelectDown()
        {
            if (_view.SelectedLine == _view.DocumentHeight - 1)
                return;

            _view.SelectedLine++;
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
            _view.TopLine = Math.Min(
                Math.Max(0, _view.DocumentHeight - _view.Height),
                _view.TopLine + _view.Height);

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
            if (_helpShowing) return;
            var i = _view.SelectedLine;
            if (i < 0)
                return;

            var nextIndex = _document.FindPreviousEntryIndex(i);
            _view.SelectedLine = _document.Entries[nextIndex].Offset;
            _view.BringIntoView(_view.SelectedLine);
        }

        private void GoNextFile()
        {
            if (_helpShowing) return;
            var i = _view.SelectedLine;
            if (i < 0)
                return;

            var nextIndex = _document.FindNextEntryIndex(i);
            _view.SelectedLine = _document.Entries[nextIndex].Offset;
            _view.BringIntoView(_view.SelectedLine);
        }

        private void GoPreviousHunk()
        {
            if (_helpShowing) return;
            var i = _view.SelectedLine;
            if (i < 0)
                return;

            _view.SelectedLine = _document.FindPreviousChangeBlock(i);
            _view.BringIntoView(_view.SelectedLine);
        }

        private void GoNextHunk()
        {
            if (_helpShowing) return;
            var i = _view.SelectedLine;
            if (i < 0)
                return;

            _view.SelectedLine = _document.FindNextChangeBlock(i);
            _view.BringIntoView(_view.SelectedLine);
        }

        private void Search()
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

        private void GoPreviousHit()
        {
            if (_view.SearchResults == null)
                return;

            var hit = _view.SearchResults.FindPrevious(_view.SelectedLine);
            if (hit != null)
                _view.SelectedLine = hit.LineIndex;
        }

        private void GoNextHit()
        {
            if (_view.SearchResults == null)
                return;

            var hit = _view.SearchResults.FindNext(_view.SelectedLine);
            if (hit != null)
                _view.SelectedLine = hit.LineIndex;
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
            if (_helpShowing) return;
            _inputLineDigits.Append(digit);
            UpdateFooter();
        }

        private void ShowHelpPage()
        {
            if (_helpShowing)
            {
                _helpShowing = false;

                UpdateRepository();

                _view.SelectedLine = _selectedLineBeforeHelpWasShown == -1 ? 0 : _selectedLineBeforeHelpWasShown;
                _view.TopLine = _topLineBeforeHelpWasShown;

                return;
            }

            _selectedLineBeforeHelpWasShown = _view.SelectedLine;
            _topLineBeforeHelpWasShown = _view.TopLine;

            _view.LineRenderer = ViewLineRenderer.Default;
            _view.Document = new HelpDocument(_commands);

            _helpShowing = true;

            UpdateHeader();
            UpdateFooter();

            _view.SelectedLine = 0;
            _view.TopLine = 0;
        }

        private void ApplyPatch(PatchDirection direction, bool entireHunk)
        {
            if (_helpShowing) return;

            if (_view.SelectedLine < 0)
                return;

            if (_viewFiles)
            {
                var change = _fileDocument.GetChange(_view.SelectedLine);
                if (change != null)
                {
                    if (direction == PatchDirection.Stage)
                        RunGit($"add \"{change.Path}\"");
                    else if (direction == PatchDirection.Unstage)
                        RunGit($"reset \"{change.Path}\"");
                    else if (direction == PatchDirection.Reset)
                        RunGit($"checkout \"{change.Path}\"");
                }
            }
            else
            {
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
            }

            UpdateRepository();
        }
    }
}
