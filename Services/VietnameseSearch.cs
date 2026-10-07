using System.Globalization;
using System.Text;
namespace Nosql_Neo4j.Services;

public static class VietnameseSearch
{
    public static string Normalize(string? text)
    {
        var value = (text ?? "").Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var result = new StringBuilder();
        foreach (var ch in value)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) result.Append(ch);
        return string.Join(' ', result.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
    public static int Rank(IEnumerable<string> names, string query)
    {
        if (query.Length == 0) return 0;
        return names.Select(Normalize).Select(n => n == query ? 0 :
            n.StartsWith(query, StringComparison.Ordinal) ? 1 : n.Contains(query, StringComparison.Ordinal) ? 2 : 3).Min();
    }
}