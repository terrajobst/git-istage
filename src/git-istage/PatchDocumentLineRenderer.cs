using System;

namespace GitIStage
{
    internal class PatchDocumentLineRenderer : ViewLineRenderer
    {
        private static PatchLine GetLine(View view, int lineIndex)
        {
            var document = view.Document as PatchDocument;
            return document?.Lines[lineIndex];
        }

        private static ConsoleColor GetForegroundColor(View view, int lineIndex)
        {
            var line = GetLine(view, lineIndex);
            if (line == null)
                return Console.ForegroundColor;

            switch (line.Kind)
            {
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
                    return Console.ForegroundColor;
            }
        }

        private static ConsoleColor GetBackgroundColor(View view, int lineIndex)
        {
            var patchLine = GetLine(view, lineIndex);
            if (patchLine == null)
                return Console.BackgroundColor;

            var kind = patchLine.Kind;

            var isSelected = view.SelectedLine == lineIndex;
            if (isSelected)
            {
                var canStage = kind == PatchLineKind.Addition ||
                               kind == PatchLineKind.Removal;
                return canStage
                    ? ConsoleColor.Gray
                    : ConsoleColor.DarkGray;
            }

            return Console.BackgroundColor;
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