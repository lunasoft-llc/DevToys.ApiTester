namespace DevToys.ApiTester.Core;

public static class KeyValueParser
{
    public static Dictionary<string, string> Parse(string text, char preferredSeparator = ':')
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string rawLine in text.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            int index = line.IndexOf(preferredSeparator);
            if (index < 1 && preferredSeparator != '=') index = line.IndexOf('=');
            if (index < 1) continue;
            result[line[..index].Trim()] = line[(index + 1)..].Trim();
        }
        return result;
    }

    public static string Format(IEnumerable<KeyValuePair<string, string>> values, char separator = ':')
        => string.Join(Environment.NewLine, values.Select(x => $"{x.Key}{separator} {x.Value}"));
}
