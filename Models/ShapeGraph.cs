namespace Nosql_Neo4j.Models;

public sealed record ShapeEdge(string SourceId, string TargetId);

public sealed record ShapeGraph(
    IReadOnlyList<ShapeSummary> Nodes,
    IReadOnlyList<ShapeEdge> Edges,
    IReadOnlyList<string[]> Paths);
