namespace GitIStage.UI;

internal sealed class SearchResults
{
    public SearchResults(Document document, string searchTerm)
    {
        var hits = new List<SearchHit>();

        for (var i = 0; i < document.Height; i++)
        {
            var line = document.GetLine(i);

            var index = line.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);
            while (index >= 0)
            {
                hits.Add(new SearchHit(i, index, searchTerm.Length));
                index = line.IndexOf(searchTerm, index + searchTerm.Length, StringComparison.OrdinalIgnoreCase);
            }
        }

        Hits = hits.ToArray();
    }

    public IReadOnlyList<SearchHit> Hits { get; }

    public SearchHit FindPrevious(int line)
    {
        for (int i = Hits.Count - 1; i >= 0; i--)
        {
            if (Hits[i].LineIndex < line)
                return Hits[i];
        }

        return null;
    }

    public SearchHit FindNext(int line)
    {
        for (int i = 0; i < Hits.Count; i++)
        {
            if (Hits[i].LineIndex > line)
                return Hits[i];
        }

        return null;
    }
}