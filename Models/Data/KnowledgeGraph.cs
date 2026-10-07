namespace Nosql_Neo4j.Models.Data;

public sealed record ShapeRecord(string Id, string Name, string[] Aliases, string? ImageUrl);
public sealed record KnowledgeRecord(string Id, string ShapeId, string Type, string Title,
    string Content, string Expression, string[] Variables, string Conditions, string Unit,
    string PropertyCode, string SourceTitle, string SourceRef);
public sealed record EdgeRecord(string SourceId, string TargetId);
public sealed record KnowledgeSnapshot(ShapeRecord[] Shapes, KnowledgeRecord[] Knowledge, EdgeRecord[] Edges);