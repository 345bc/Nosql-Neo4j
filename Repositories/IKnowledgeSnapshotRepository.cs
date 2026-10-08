using Nosql_Neo4j.Models.Data;
namespace Nosql_Neo4j.Repositories;

public interface IKnowledgeSnapshotRepository
{
    Task<KnowledgeSnapshot> ReadPublishedAsync(CancellationToken ct);
}