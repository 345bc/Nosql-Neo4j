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
        IReadOnlyList<ShapeFormula> Formulas);
}
