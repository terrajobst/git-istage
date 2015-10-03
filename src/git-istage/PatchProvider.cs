using System;

using LibGit2Sharp;

namespace GitIStage
{
    internal sealed class PatchProvider
    {
        private readonly Repository _repository;
        private readonly bool _isStage;

        public PatchProvider(Repository repository, string path, bool isStage)
        {
            _repository = repository;
            _isStage = isStage;
            Path = path;
        }

        public string Path { get; }

        public Patch GetPatch()
        {
            var paths = new[] {Path};
            return _isStage 
                ? _repository.Diff.Compare<Patch>(_repository.Head.Tip.Tree, DiffTargets.Index, paths)
                : _repository.Diff.Compare<Patch>(paths, true);
        }
    }
}