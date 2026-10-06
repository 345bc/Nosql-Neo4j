using Nosql_Neo4j.Models;
namespace Nosql_Neo4j.Models.ViewModels;

public sealed class DraftShapesViewModel
{
    public IReadOnlyList<ShapeSummary> Shapes { get; init; } = [];
    public string? ErrorMessage { get; init; }
}
public sealed class DraftShapeDetailsViewModel
{
    public ShapeDetail? Shape { get; init; }
    public string? ErrorMessage { get; init; }
}
public sealed class DraftGraphViewModel
{
    public ShapeGraph? Graph { get; init; }
    public string? ErrorMessage { get; init; }
    public string NameOf(string id)
        => Graph?.Nodes.FirstOrDefault(n => n.Id == id)?.Name ?? id;
}
public sealed record ErrorViewModel(string? RequestId)
{
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
