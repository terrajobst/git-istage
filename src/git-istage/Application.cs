using System;
using System.Collections.Generic;
using System.Linq;

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
                new ConsoleCommand(IncreaseContext, ConsoleKey.OemPlus),
                new ConsoleCommand(DecreaseContext, ConsoleKey.OemMinus),
                new ConsoleCommand(ToogleFullDiff, ConsoleKey.Oem7),
                new ConsoleCommand(GoHome, ConsoleKey.Home),
                new ConsoleCommand(GoEnd, ConsoleKey.End),
                new ConsoleCommand(SelectUp, ConsoleKey.UpArrow),
                new ConsoleCommand(SelectDown, ConsoleKey.DownArrow),
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
                new ConsoleCommand(Toggle, ConsoleKey.T)
            };

            var isCursorVisible = Console.CursorVisible;
            Console.CursorVisible = false;
            Console.Clear();

            _header = new Label(0, 0, Console.WindowWidth);
            _header.Foreground = ConsoleColor.Yellow;
            _header.Background = ConsoleColor.DarkGray;

            var renderer = new PatchDocumentLineRenderer();
            _view = new View(renderer, 1, 0, Console.WindowHeight - 2, Console.WindowWidth);
            _view.SelectedLineChanged += delegate { UpdateHeader(); };

            _footer = new Label(Console.WindowHeight - 2, 0, Console.WindowWidth);
            _footer.Foreground = ConsoleColor.Yellow;
            _footer.Background = ConsoleColor.DarkGray;

            UpdateRepository();

            using (_repository)
            {
                while (!_done)
                {
                    var key = Console.ReadKey(true);
                    var command = commands.FirstOrDefault(c => c.MatchesKey(key));
                    command?.Execute();
                }

                Console.Clear();
                Console.CursorVisible = isCursorVisible;
            }
        }
       
        private void Toggle()
        {
            _viewStage = !_viewStage;

            UpdateRepository();
        }

        private void UpdateRepository()
        {
            _repository?.Dispose();
            _repository = new Repository(_repositoryPath);

            var compareOptions = new CompareOptions();
            compareOptions.ContextLines = _fullFileDiff ? int.MaxValue : _contextLines;

            var changes = _viewStage
                ? _repository.Diff.Compare<TreeChanges>(_repository.Head.Tip.Tree, DiffTargets.Index)
                : _repository.Diff.Compare<TreeChanges>(null, true);
            var paths = changes.Select(c => c.Path).ToArray();
            var patch = paths.Any()
                ? _viewStage
                    ? _repository.Diff.Compare<Patch>(_repository.Head.Tip.Tree, DiffTargets.Index, paths, null, compareOptions)
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
            var stageChanges = _repository.Diff.Compare<TreeChanges>(_repository.Head.Tip.Tree, DiffTargets.Index);
            var stageAdded = stageChanges.Added.Count();
            var stageModified = stageChanges.Modified.Count();
            var stageDeleted = stageChanges.Deleted.Count();

            var workingChanges = _repository.Diff.Compare<TreeChanges>(null, true);
            var workingAdded = workingChanges.Added.Count();
            var workingModified = workingChanges.Modified.Count();
            var workingDeleted = workingChanges.Deleted.Count();

            _footer.Text = $" [{_repository.Head.FriendlyName} +{stageAdded} ~{stageModified} -{stageDeleted} | +{workingAdded} ~{workingModified} -{workingDeleted}]";
        }

        private void Exit()
        {
            _done = true;
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

        private void GoHome()
        {
            _view.LeftChar = 0;
            _view.SelectedLine = 0;
        }

        private void GoEnd()
        {
            _view.LeftChar = 0;
            _view.SelectedLine = _view.DocumentHeight - 1;
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