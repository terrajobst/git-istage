using System;

namespace GitIStage
{
    internal sealed class PatchDocumentLineRenderer : ViewLineRenderer
    {
        public static new PatchDocumentLineRenderer Default { get; } = new PatchDocumentLineRenderer();

        private static PatchLine GetLine(View view, int lineIndex)
        {
            var document = view.Document as PatchDocument;
            return document?.Lines[lineIndex];
        }

        private static ConsoleColor GetForegroundColor(View view, int lineIndex)
        {
            var line = GetLine(view, lineIndex);
            if (line == null)
                return ConsoleColor.Gray;

            switch (line.Kind)
            {
                case PatchLineKind.DiffLine:
                    return ConsoleColor.Yellow;
                case PatchLineKind.Header:
                    return ConsoleColor.White;
                case PatchLineKind.Hunk:
                    return ConsoleColor.DarkCyan;
                case PatchLineKind.Context:
                    goto default;
                case PatchLineKind.Addition:
                    return ConsoleColor.DarkGreen;
                case PatchLineKind.Removal:
                    return ConsoleColor.DarkRed;
                default:
                    return ConsoleColor.Gray;
            }
        }

        private static ConsoleColor GetBackgroundColor(View view, int lineIndex)
        {
            var patchLine = GetLine(view, lineIndex);
            if (patchLine == null)
                return ConsoleColor.Black;

            var kind = patchLine.Kind;

            var isSelected = view.SelectedLine == lineIndex;
            if (isSelected)
            {
                return kind.IsAdditionOrRemoval()
                    ? ConsoleColor.Gray
                    : ConsoleColor.DarkGray;
            }

            return kind == PatchLineKind.DiffLine ? ConsoleColor.DarkBlue : ConsoleColor.Black;
        }

        public override void Render(View view, int lineIndex)
        {
            var line = GetLine(view, lineIndex);
            if (line == null)
                return;

            var foregroundColor = GetForegroundColor(view, lineIndex);
            var backgroundColor = GetBackgroundColor(view, lineIndex);

            RenderLine(view, lineIndex, line.Text, foregroundColor, backgroundColor);
        }
    }
}