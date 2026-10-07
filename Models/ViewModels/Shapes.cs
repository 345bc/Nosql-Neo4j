namespace Nosql_Neo4j.Contracts;

public sealed record ShapeSummaryVm(string Id, string Name, string[] Aliases, string? ImageUrl);
public sealed record FormulaVm(string Name, string Expression, string Variables, string Conditions);
public sealed record SourceVm(string Title, string Locator);
public sealed record ShapeDetailVm(ShapeSummaryVm Summary, string Definition,
    string[] Properties, string[] RecognitionSigns, FormulaVm[] Formulas,
    string[] Examples, SourceVm[] Sources, string? Convention);
public sealed record ShapeListVm(ShapeSummaryVm[] Items, int Page, int PageSize, int TotalItems);
public sealed record GraphNodeVm(string Id, string Name);
public sealed record GraphEdgeVm(string SourceId, string TargetId, string Type);
public sealed record GraphVm(GraphNodeVm[] Nodes, GraphEdgeVm[] Edges, string[][] Paths, string Convention);