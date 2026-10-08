using Nosql_Neo4j.Contracts;

namespace Nosql_Neo4j.Models.ViewModels;

public sealed record ShapeIndexViewModel(string? Q, ShapeListVm? Result = null, string? ErrorMessage = null);
public sealed record ShapeDetailsViewModel(ShapeDetailVm? Detail = null, string? ErrorMessage = null);
public sealed record StatusViewModel(int Status, string Message);

/// <summary>Presentation data for the SVG; knowledge and path calculation stay in IShapeService.</summary>
public sealed class ShapeGraphViewModel
{
    public GraphVm? Graph { get; private init; }
    public string? FromId { get; private init; }
    public string? ToId { get; private init; }
    public string? ErrorMessage { get; private init; }
    public Dictionary<string, (int X, int Y)> Positions { get; } = [];
    public HashSet<string> PathNodes { get; } = [];
    public HashSet<(string, string)> PathEdges { get; } = [];
    public int CanvasWidth { get; private set; } = 900;
    public int CanvasHeight { get; private set; } = 540;

    public static ShapeGraphViewModel Create(GraphVm? graph, string? fromId, string? toId, string? errorMessage)
    {
        var model = new ShapeGraphViewModel { Graph = graph, FromId = fromId, ToId = toId, ErrorMessage = errorMessage };
        if (graph is null) return model;
        // IShapeService validates acyclicity, endpoints and depth before returning the graph.
        var levels = new Dictionary<string, int>();
        int Level(string id)
        {
            if (levels.TryGetValue(id, out var level)) return level;
            var parents = graph.Edges.Where(e => e.SourceId == id).Select(e => e.TargetId).ToArray();
            return levels[id] = parents.Length == 0 ? 0 : parents.Max(Level) + 1;
        }
        foreach (var node in graph.Nodes) Level(node.Id);
        var rows = graph.Nodes.GroupBy(n => levels[n.Id]).OrderBy(g => g.Key).ToArray();
        model.CanvasWidth = Math.Max(900, rows.Length == 0 ? 900 : rows.Max(row => row.Count()) * 210);
        foreach (var row in rows)
        {
            var nodes = row.OrderBy(n => n.Id, StringComparer.Ordinal).ToArray();
            for (int i = 0; i < nodes.Length; i++)
                model.Positions[nodes[i].Id] = ((i + 1) * model.CanvasWidth / (nodes.Length + 1), 55 + row.Key * 100);
        }
        model.CanvasHeight = levels.Count == 0 ? 200 : Math.Max(240, levels.Values.Max() * 100 + 110);
        foreach (var path in graph.Paths)
        {
            foreach (var id in path) model.PathNodes.Add(id);
            for (int i = 0; i + 1 < path.Length; i++) model.PathEdges.Add((path[i], path[i + 1]));
        }
        return model;
    }
}