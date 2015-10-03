using System;

namespace GitIStage
{
    internal abstract class Document
    {
        public static readonly Document Empty = new EmptyDocument();

        public abstract int Height { get; }
        public abstract int Width { get; }

        private sealed class EmptyDocument : Document
        {
            public override int Height => 0;
            public override int Width => 0;
        }
    }
}