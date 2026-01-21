namespace GitIStage.Text;

internal sealed partial class StyledSpanTree
{
    private sealed class StyledSpanNode
    {
        public StyledSpanNode(StyledSpan styledSpan)
        {
            StyledSpan = styledSpan;
            Height = 1;
        }

        public StyledSpan StyledSpan { get; set; }

        public TextStyle Style => StyledSpan.Style;

        public TextSpan Span => StyledSpan.Span;

        public StyledSpanNode? Left { get; set; }

        public StyledSpanNode? Right { get; set; }

        public int Height { get; set; }
    }
}