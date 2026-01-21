namespace GitIStage.Text;

public sealed class StyleSpanBuilder
{
    private readonly StyledSpanTree _tree = new();
    private readonly List<StyledSpan> _existingSpans = new();

    public void Clear()
    {
        _tree.Clear();
    }

    public void Clear(TextSpan span)
    {
        Set(span, null);
    }

    public void Add(StyledSpan span)
    {
        Set(span.Span, span.Style);
    }

    private void Set(TextSpan span, TextStyle? style)
    {
        _existingSpans.Clear();
        _tree.FindSpans(span, _existingSpans);

        if (_existingSpans.Count == 0)
        {
            if (style is not null)
                _tree.Insert(new StyledSpan(span, style.Value));
            return;
        }

        foreach (var existingSpan in _existingSpans)
        {
            _tree.Remove(existingSpan.Span);

            var newSpanStart = int.Max(span.Start, existingSpan.Span.Start);
            var newSpanEnd = int.Min(span.End, existingSpan.Span.End);
            var newSpan = TextSpan.FromBounds(newSpanStart, newSpanEnd);

            // First let's see what we need to at the beginning

            if (existingSpan.Span.Start < newSpan.Start)
            {
                //      [-- New
                //   [----- Existing

                var fragmentStart = existingSpan.Span.Start;
                var fragmentEnd = newSpan.Start;
                var fragmentSpan = TextSpan.FromBounds(fragmentStart, fragmentEnd);
                var fragment = new StyledSpan(fragmentSpan, existingSpan.Style);
                _tree.Insert(fragment);

            }
            else if (existingSpan.Span.Start > newSpan.Start)
            {
                //  [------ New
                //     [--- Existing

                if (style is not null)
                {
                    var fragmentStart = newSpan.Start;
                    var fragmentEnd = existingSpan.Span.Start;
                    var fragmentSpan = TextSpan.FromBounds(fragmentStart, fragmentEnd);
                    var fragment = new StyledSpan(fragmentSpan, style.Value);
                    _tree.Insert(fragment);
                }
            }
            else
            {
                //  [------ New
                //  [------ Existing
                //
                // Nothing to do at the beginning.
            }

            // Let's see what happens in the middle

            var commonStart = int.Max(existingSpan.Span.Start, newSpan.Start);
            var commonEnd = int.Min(existingSpan.Span.End, newSpan.End);
            if (commonStart < commonEnd)
            {
                //    [-------]     New
                //  --------------- Existing
                //
                //  Or
                //
                //  --------------- New
                //    [-------]     Existing

                if (style is not null)
                {
                    var commonSpan = TextSpan.FromBounds(commonStart, commonEnd);
                    var commonStyle = style.Value.PlaceOnTopOf(existingSpan.Style);
                    var common = new StyledSpan(commonSpan, commonStyle);
                    _tree.Insert(common);
                }
            }

            // Now let's see what we need to do at the end

            if (existingSpan.Span.End < newSpan.End)
            {
                //  ------] New
                //  ---]    Existing

                if (style is not null)
                {
                    var fragmentStart = existingSpan.Span.End;
                    var fragmentEnd = newSpan.End;
                    var fragmentSpan = TextSpan.FromBounds(fragmentStart, fragmentEnd);
                    var fragment = new StyledSpan(fragmentSpan, style.Value);
                    _tree.Insert(fragment);
                }
            }
            else if (existingSpan.Span.End > newSpan.End)
            {
                //   --]    New
                //   -----] Existing

                var fragmentStart = newSpan.End;
                var fragmentEnd = existingSpan.Span.End;
                var fragmentSpan = TextSpan.FromBounds(fragmentStart, fragmentEnd);
                var fragment = new StyledSpan(fragmentSpan, existingSpan.Style);
                _tree.Insert(fragment);
            }
            else
            {
                //  ------] New
                //  ------] Existing
                //
                // Nothing to do at the end.
            }
        }
    }

    public void AddRange(IEnumerable<StyledSpan> spans)
    {
        foreach (var span in spans)
            Add(span);
    }

    private static TextStyle CombineStyles(TextStyle bottomStyle, TextStyle topStyle)
    {
        return topStyle.PlaceOnTopOf(bottomStyle);
    }

    public IEnumerable<StyledSpan> GetSpans()
    {
        return _tree;
    }
}
