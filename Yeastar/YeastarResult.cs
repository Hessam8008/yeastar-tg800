namespace Yeastar;

public class YeastarResult
{
    public record YeastarResultRow(string Property, string Value)
    {
        public bool Is(string compareWith)
            => Value.Equals(compareWith, StringComparison.InvariantCultureIgnoreCase);
    };

    public static YeastarResult Process(string response)
    {
        var result = new YeastarResult();

        var arr = response.Split("\n");
        foreach (var s in arr)
        {
            var k = s.Split(':', 2, StringSplitOptions.TrimEntries);
            if (!string.IsNullOrEmpty(k[0]))
                result.Add(k[0].Trim(), k.Length > 1 ? k[1].Trim() : "N/A");
        }

        return result;
    }

    private readonly List<YeastarResultRow> _rows = [];

    public void Add(string property, string value)
    {
        _rows.Add(new YeastarResultRow(property, value));
    }

    public YeastarResultRow? Find(string property)
        => _rows.FirstOrDefault(x => x.Property.Equals(property, StringComparison.InvariantCultureIgnoreCase));

    public bool Exists(string property, string value)
        => Find(property)?.Value.Equals(value, StringComparison.InvariantCultureIgnoreCase) ?? false;
}