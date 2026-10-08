namespace Nosql_Neo4j.Models
{
    public sealed record ShapeFormula(
     string Id,
     string Name,
     string Expression,
     string Variables,
     string Conditions);

    public sealed record ShapeDetail(
        string Id,
        string Name,
        string Definition,
        string[] Properties,
        string[] RecognitionSigns,
        string[] Examples,
        IReadOnlyList<ShapeFormula> Formulas)
    {
        public string SourceTitle { get; init; } = "";
        public string SourceLocator { get; init; } = "";
        public string ReviewedBy { get; init; } = "";
        public string ReviewedAt { get; init; } = "";
        public string Convention { get; init; } = "";
    }
}
