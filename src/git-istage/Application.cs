using System;
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

        public void Run()
        {
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
                new ConsoleCommand(Unstage, ConsoleKey.U),
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

            var paths = changes.Select(c => c.Path);
            var patches = from p in paths
                          let singlePath = new[] {p}
                          let patch = _viewStage
                              ? _repository.Diff.Compare<Patch>(_repository.Head.Tip.Tree, DiffTargets.Index, singlePath)
                              : _repository.Diff.Compare<Patch>(singlePath, true)
                          select new PatchFile(patch, p);
            _document = new PatchDocument(patches);
            _view.Document = _document;

            UpdateHeader();
            UpdateFooter();
        }

        private void UpdateHeader()
        {
            var line = _document.Lines.Any() ? _document.Lines[_view.SelectedLine] : null;
            var emptyMarker = _viewStage ? "*nothing to commit*" : "*clean*";
            var path = line == null ? emptyMarker : line.PatchFile.Path;
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

            var file = _document.Lines[i].PatchFile;
            var fileIndex = _document.Files.IndexOf(file);
            var previousFile = fileIndex == 0
                ? file
                : _document.Files[fileIndex - 1];
            var firstLine = _document.Lines.IndexOf(previousFile.Lines.First());
            _view.TopLine = _view.SelectedLine = firstLine;
        }

        private void ShowNextFile()
        {
            var i = _view.SelectedLine;
            if (i < 0)
                return;

            var file = _document.Lines[i].PatchFile;
            var fileIndex = _document.Files.IndexOf(file);
            var nextFile = fileIndex == _document.Files.Count - 1
                ? file
                : _document.Files[fileIndex + 1];
            var firstLine = _document.Lines.IndexOf(nextFile.Lines.First());
            _view.SelectedLine = firstLine;
            _view.TopLine = Math.Max(0, Math.Min(firstLine, _view.DocumentHeight - _view.Height));
        }

        private void Stage()
        {
            if (_viewStage)
                return;

            StageUnstage();
        }

        private void Unstage()
        {
            if (!_viewStage)
                return;

            StageUnstage();
        }

        private void StageUnstage()
        {
            var line = _document.Lines[_view.SelectedLine];
            if (line.Kind != PatchLineKind.Addition &&
                line.Kind != PatchLineKind.Removal)
                return;

            var patchFile = line.PatchFile;
            var index = line.Index;
            var patch = Patching.Stage(patchFile, index, _viewStage);

            Patching.ApplyPatch(_repository.Info.WorkingDirectory, patch, _viewStage);
            UpdateRepository();
        }
    }
}