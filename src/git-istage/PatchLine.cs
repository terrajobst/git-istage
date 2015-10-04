using System;

namespace GitIStage
{
    internal sealed class PatchLine
    {
        public PatchLine(PatchLineKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }

        public PatchLineKind Kind { get; }

        public string Text { get; }

        public override string ToString()
        {
            var kindStr = Kind.ToString();
            return $"[{kindStr,-12}] {Text}";
        }
    }
}