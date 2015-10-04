using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using LibGit2Sharp;

namespace GitIStage
{
    internal sealed class Application
    {
        private bool _done;

        private Repository _repository;
        private bool _viewStage;
        private Label _header;
        private View _view;
        private Label _footer;
        private PatchDocument _document;

        public void Run(string repositoryPath)
        {
            if (!Repository.IsValid(repositoryPath))
            {
                Console.WriteLine("Not a git repository");
                return;
            }

            var commands = new[]
            {
                new ConsoleCommand(Exit, ConsoleKey.Escape),
                new ConsoleCommand(Exit, ConsoleKey.Q),
                new ConsoleCommand(GoHome, ConsoleKey.Home),
                new ConsoleCommand(GoEnd, ConsoleKey.End),
                new ConsoleCommand(SelectUp, ConsoleKey.UpArrow),
                new ConsoleCommand(SelectDown, ConsoleKey.DownArrow),
                new ConsoleCommand(ScrollUp, ConsoleKey.UpArrow, ConsoleModifiers.Control),
                new ConsoleCommand(ScrollDown, ConsoleKey.DownArrow, ConsoleModifiers.Control),
                new ConsoleCommand(ScrollLeft, ConsoleKey.LeftArrow, ConsoleModifiers.Control),
                new ConsoleCommand(ScrollRight, ConsoleKey.RightArrow, ConsoleModifiers.Control),
                new ConsoleCommand(ShowPreviousFile, ConsoleKey.LeftArrow), 
                new ConsoleCommand(ShowNextFile, ConsoleKey.RightArrow),
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
            _repository = new Repository(Directory.GetCurrentDirectory());

            var changes = _viewStage
                ? _repository.Diff.Compare<TreeChanges>(_repository.Head.Tip.Tree, DiffTargets.Index)
                : _repository.Diff.Compare<TreeChanges>(null, true);
            var paths = changes.Select(c => c.Path).ToArray();
            var patch = paths.Any()
                ? _viewStage
                    ? _repository.Diff.Compare<Patch>(_repository.Head.Tip.Tree, DiffTargets.Index, paths)
                    : _repository.Diff.Compare<Patch>(paths, true)
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

        private void ShowPreviousFile()
        {
            var i = _view.SelectedLine;
            if (i < 0)
                return;

            var entryIndex = _document.FindEntryIndex(i);
            var previousEntry = entryIndex == 0
                ? entryIndex
                : entryIndex - 1;
            var firstLine = _document.Entries[previousEntry].Offset;
            _view.TopLine = _view.SelectedLine = firstLine;
        }

        private void ShowNextFile()
        {
            var i = _view.SelectedLine;
            if (i < 0)
                return;

            var entryIndex = _document.FindEntryIndex(i);
            var nextEntry = entryIndex == _document.Entries.Count - 1
                ? entryIndex
                : entryIndex + 1;
            var firstLine = _document.Entries[nextEntry].Offset;
            _view.SelectedLine = firstLine;
            _view.TopLine = Math.Max(0, Math.Min(firstLine, _view.DocumentHeight - _view.Height));
        }

        private void Stage()
        {
            if (_viewStage)
                return;

            StageUnstage(false);
        }

        private void StageHunk()
        {
            if (_viewStage)
                return;

            StageUnstage(true);
        }

        private void Unstage()
        {
            if (!_viewStage)
                return;

            StageUnstage(false);
        }

        private void UnstageHunk()
        {
            if (!_viewStage)
                return;

            StageUnstage(true);
        }

        private void StageUnstage(bool entireHunk)
        {
            if (_view.SelectedLine < 0)
                return;

            var line = _document.Lines[_view.SelectedLine];
            if (line.Kind != PatchLineKind.Addition &&
                line.Kind != PatchLineKind.Removal)
                return;

            IEnumerable<int> lines;
            if (!entireHunk)
            {
                lines = new[] {_view.SelectedLine};
            }
            else
            {
                var entry = _document.FindEntry(_view.SelectedLine);
                var hunk = entry.FindHunk(_view.SelectedLine);
                lines = Enumerable.Range(hunk.Offset, hunk.Length)
                                  .Where(i => _document.Lines[i].Kind == PatchLineKind.Addition ||
                                              _document.Lines[i].Kind == PatchLineKind.Removal);
            }
            var patch = Patching.Stage(_document, lines, _viewStage);

            Patching.ApplyPatch(_repository.Info.WorkingDirectory, patch, _viewStage);
            UpdateRepository();
        }
    }
}