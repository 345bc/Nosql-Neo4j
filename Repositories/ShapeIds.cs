namespace Nosql_Neo4j.Repositories;
/// <summary>Translate legacy seed codes to the stable route IDs agreed by the team.</summary>
public static class ShapeIds
{
    private static readonly IReadOnlyDictionary<string, string> Legacy = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["TuGiac"] = "TU_GIAC",
        ["HinhThang"] = "HINH_THANG",
        ["HinhBinhHanh"] = "HINH_BINH_HANH",
        ["HinhChuNhat"] = "HINH_CHU_NHAT",
        ["HinhThoi"] = "HINH_THOI",
        ["HinhVuong"] = "HINH_VUONG"
    };
    public static string Canonical(string id) => Legacy.TryGetValue(id, out var canonical) ? canonical : id;
    public static string? Illustration(string id)
    {
        var canonical = Canonical(id);
        return Legacy.Values.Contains(canonical) ? $"/images/{canonical.ToLowerInvariant()}.svg" : null;
    }
}