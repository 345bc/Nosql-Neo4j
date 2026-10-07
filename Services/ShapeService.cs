using Nosql_Neo4j.Contracts;
using Nosql_Neo4j.Models.Data;
using Nosql_Neo4j.Repositories;
namespace Nosql_Neo4j.Services;

public sealed class ShapeService(IKnowledgeSnapshotRepository repository) : IShapeService
{
    public const string Convention = "Phạm vi: tứ giác đơn, lồi, không suy biến. Hình thang có ít nhất một cặp cạnh đối song song; hình bình hành cũng là hình thang. Mũi tên IS_A đi từ hình con đến hình cha.";
    public async Task<ShapeListVm> ListAsync(string? q, int page, CancellationToken ct)
    {
        if (page < 1) throw Invalid("page", "Số trang phải lớn hơn hoặc bằng 1.");
        if ((q?.Length ?? 0) > 100) throw Invalid("q", "Từ khóa tối đa 100 ký tự.");
        var snapshot = await repository.ReadPublishedAsync(ct);
        var query = VietnameseSearch.Normalize(q);
        var found = snapshot.Shapes.Select(s => (shape: s, rank: VietnameseSearch.Rank(new[] { s.Name }.Concat(s.Aliases), query)))
            .Where(x => x.rank < 3).OrderBy(x => x.rank)
            .ThenBy(x => VietnameseSearch.Normalize(x.shape.Name), StringComparer.Ordinal)
            .ThenBy(x => x.shape.Id, StringComparer.Ordinal).Select(x => Summary(x.shape)).ToArray();
        var offset = ((long)page - 1) * 20;
        return new(found.Skip((int)Math.Min(offset, found.Length)).Take(20).ToArray(), page, 20, found.Length);
    }
    public async Task<ShapeDetailVm?> GetDetailAsync(string id, CancellationToken ct)
    {
        var snapshot = await repository.ReadPublishedAsync(ct);
        var shape = snapshot.Shapes.SingleOrDefault(s => s.Id == id);
        if (shape is null) return null;
        var own = snapshot.Knowledge.Where(k => k.ShapeId == id).OrderBy(k => k.Id, StringComparer.Ordinal).ToArray();
        var ancestors = Ancestors(id, snapshot);
        var names = snapshot.Shapes.ToDictionary(s => s.Id, s => s.Name);
        var properties = snapshot.Knowledge.Where(k => k.Type == "PROPERTY" && (k.ShapeId == id || ancestors.Contains(k.ShapeId)))
            .OrderBy(k => k.ShapeId == id ? 0 : 1).ThenBy(k => k.Id, StringComparer.Ordinal)
            .GroupBy(k => string.IsNullOrWhiteSpace(k.PropertyCode) ? k.Id : k.PropertyCode)
            .Select(g => g.First()).Select(k => WithConditions(k) + (k.ShapeId == id ? "" : $" (Kế thừa từ {names[k.ShapeId]}.)")).ToArray();
        var used = own.Concat(snapshot.Knowledge.Where(k => k.Type == "PROPERTY" && ancestors.Contains(k.ShapeId))).ToArray();
        return new(Summary(shape), string.Join(" ", own.Where(k => k.Type == "DEFINITION").Select(WithConditions)),
            properties, own.Where(k => k.Type == "RECOGNITION").Select(WithConditions).ToArray(),
            own.Where(k => k.Type == "FORMULA").Select(k => new FormulaVm(k.Title, k.Expression,
                string.Join("; ", k.Variables) + (k.Unit.Length > 0 ? $". Đơn vị: {k.Unit}" : ""), k.Conditions)).ToArray(),
            own.Where(k => k.Type == "EXAMPLE").Select(WithConditions).ToArray(),
            used.Where(k => !string.IsNullOrWhiteSpace(k.SourceRef)).Select(k => new SourceVm(k.SourceTitle, k.SourceRef)).Distinct().ToArray(), Convention);
    }
    public async Task<GraphVm> GetGraphAsync(string? fromId, string? toId, CancellationToken ct)
    {
        var snapshot = await repository.ReadPublishedAsync(ct);
        ValidateGraph(snapshot);
        bool hasFrom = !string.IsNullOrWhiteSpace(fromId), hasTo = !string.IsNullOrWhiteSpace(toId);
        if (hasFrom != hasTo) throw Invalid(hasFrom ? "toId" : "fromId", "Chọn cả hình bắt đầu và hình đích.");
        if (hasFrom && !snapshot.Shapes.Any(s => s.Id == fromId)) throw Invalid("fromId", "Hình bắt đầu không tồn tại hoặc chưa công bố.");
        if (hasTo && !snapshot.Shapes.Any(s => s.Id == toId)) throw Invalid("toId", "Hình đích không tồn tại hoặc chưa công bố.");
        var paths = hasFrom ? Paths(fromId!, toId!, snapshot) : [];
        return new(snapshot.Shapes.Select(s => new GraphNodeVm(s.Id, s.Name)).ToArray(),
            snapshot.Edges.Select(e => new GraphEdgeVm(e.SourceId, e.TargetId, "IS_A")).ToArray(), paths, Convention);
    }
    private static ShapeSummaryVm Summary(ShapeRecord s) => new(s.Id, s.Name, s.Aliases, s.ImageUrl);
    private static string WithConditions(KnowledgeRecord k) => k.Content + (k.Conditions.Length > 0 ? $" Điều kiện: {k.Conditions}" : "");
    private static DomainValidationException Invalid(string field, string message) => new("INVALID_INPUT", message, new Dictionary<string, string[]> { { field, [message] } });
    private static HashSet<string> Ancestors(string id, KnowledgeSnapshot graph)
    {
        ValidateGraph(graph);
        var found = new HashSet<string>();
        void Visit(string child, int depth)
        {
            foreach (var e in graph.Edges.Where(e => e.SourceId == child))
            {
                if (depth >= 10) throw Invalid("", "Độ sâu phân loại vượt giới hạn 10 cạnh.");
                if (found.Add(e.TargetId)) Visit(e.TargetId, depth + 1);
            }
        }
        Visit(id, 0); return found;
    }
    private static void ValidateGraph(KnowledgeSnapshot graph)
    {
        if (graph.Shapes.Length > 100) throw Invalid("", "Đồ thị vượt giới hạn 100 hình.");
        var ids = graph.Shapes.Select(s => s.Id).ToHashSet();
        if (ids.Count != graph.Shapes.Length || graph.Edges.Any(e => !ids.Contains(e.SourceId) || !ids.Contains(e.TargetId)))
            throw Invalid("", "Dữ liệu phân loại không hợp lệ.");
        var colors = new Dictionary<string, int>();
        void Visit(string id)
        {
            if (colors.GetValueOrDefault(id) == 1) throw Invalid("", "Đồ thị có chu trình hoặc cạnh tự nối; cần quản trị viên sửa dữ liệu.");
            if (colors.GetValueOrDefault(id) == 2) return;
            colors[id] = 1; foreach (var e in graph.Edges.Where(e => e.SourceId == id)) Visit(e.TargetId); colors[id] = 2;
        }
        foreach (var id in ids) Visit(id);
        var depths = new Dictionary<string, int>();
        int Depth(string id)
        {
            if (depths.TryGetValue(id, out var known)) return known;
            var parents = graph.Edges.Where(e => e.SourceId == id).Select(e => e.TargetId).ToArray();
            var value = parents.Length == 0 ? 0 : parents.Max(Depth) + 1;
            if (value > 10) throw Invalid("", "Độ sâu phân loại vượt giới hạn 10 cạnh.");
            return depths[id] = value;
        }
        foreach (var id in ids) Depth(id);
    }
    private static string[][] Paths(string from, string to, KnowledgeSnapshot graph)
    {
        var results = new List<string[]>();
        void Visit(List<string> path)
        {
            if (path[^1] == to) { results.Add(path.ToArray()); return; }
            var next = graph.Edges.Where(e => e.SourceId == path[^1]).Select(e => e.TargetId).Distinct().Order(StringComparer.Ordinal).ToArray();
            if (next.Length > 0 && path.Count - 1 >= 10) throw Invalid("", "Đường đi vượt giới hạn 10 cạnh.");
            foreach (var id in next) { path.Add(id); Visit(path); path.RemoveAt(path.Count - 1); }
            if (results.Count > 1000) throw Invalid("", "Có quá nhiều đường đi; hãy chọn hai hình gần hơn.");
        }
        Visit([from]); return results.OrderBy(p => p.Length).ThenBy(p => string.Join('/', p), StringComparer.Ordinal).ToArray();
    }
}