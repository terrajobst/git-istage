using System.Collections.Generic;

namespace GitIStage
{
    internal class Shortcuts
    {
        public List<PatchLine> Get()
        {
            return new List<PatchLine>
            {
                new PatchLine(PatchLineKind.Context, "Esc | Return to command line."),
                new PatchLine(PatchLineKind.Context, "Q | Return to command line."),
                new PatchLine(PatchLineKind.Context, "C | Author commit"),
                new PatchLine(PatchLineKind.Context, "Alt C | Authors commit with --amend option"),
                new PatchLine(PatchLineKind.Context, "Alt S | Stashes changes from the working copy, but leaves the stage as-is."),
                new PatchLine(PatchLineKind.Context, "T | Toggle between working copy changes and staged changes."),
                new PatchLine(PatchLineKind.Context, "R | When viewing the working copy, removes the selected line from the working copy."),
                new PatchLine(PatchLineKind.Context, "S | When viewing the working copy, stages the selected line."),
                new PatchLine(PatchLineKind.Context, "U | When viewing the stage, unstages the selected line."),
                new PatchLine(PatchLineKind.Context, "Shift R | When viewing the working copy, removes the selected block from the working copy."),
                new PatchLine(PatchLineKind.Context, "Shift S | When viewing the working copy, stages the selected block."),
                new PatchLine(PatchLineKind.Context, "Shift U | When viewing the stage, unstages the selected block."),
                new PatchLine(PatchLineKind.Context, "+ | Increases the number of contextual lines."),
                new PatchLine(PatchLineKind.Context, "- | Decreases the number of contextual lines."),
                new PatchLine(PatchLineKind.Context, "\\ | Toggles between viewing entire files and changes only."),
                new PatchLine(PatchLineKind.Context, "W | Toggles between showing and hiding whitespace."),
                new PatchLine(PatchLineKind.Context, "Up | Selects the previous line."),
                new PatchLine(PatchLineKind.Context, "Down | Selects the next line."),
                new PatchLine(PatchLineKind.Context, "Left | Go to the previous file."),
                new PatchLine(PatchLineKind.Context, "Right | Go to the next file."),
                new PatchLine(PatchLineKind.Context, "[ | Go to previous change block."),
                new PatchLine(PatchLineKind.Context, "] | Go to next change block."),
                new PatchLine(PatchLineKind.Context, "PgUp | Selects the line one screen above."),
                new PatchLine(PatchLineKind.Context, "PgDown | Selects the line one screen below."),
                new PatchLine(PatchLineKind.Context, "Space | Selects the line one screen below."),
                new PatchLine(PatchLineKind.Context, "Home | Selects the first line."),
                new PatchLine(PatchLineKind.Context, "End | Selects the last line."),
                new PatchLine(PatchLineKind.Context, "Ctrl PgUp | Scrolls up by one screen."),
                new PatchLine(PatchLineKind.Context, "Ctrl PgDown | Scrolls down by one screen."),
                new PatchLine(PatchLineKind.Context, "Ctrl Left | Scrolls left by one character,"),
                new PatchLine(PatchLineKind.Context, "Ctrl Right | Scrolls right by one character,"),
                new PatchLine(PatchLineKind.Context, "Ctrl Up | Scrolls up by one line."),
                new PatchLine(PatchLineKind.Context, "Ctrl Down | Scrolls down by one line.")
            };
        }
    }
}