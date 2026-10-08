namespace Nosql_Neo4j.Contracts;

public interface IShapeService
{
    Task<ShapeListVm> ListAsync(string? q, int page, CancellationToken ct);
    Task<ShapeDetailVm?> GetDetailAsync(string id, CancellationToken ct);
    Task<GraphVm> GetGraphAsync(string? fromId, string? toId, CancellationToken ct);
}
public sealed class DomainValidationException : Exception
{
    public string Code { get; }
    public IReadOnlyDictionary<string, string[]> Errors { get; }
    public DomainValidationException(string code, string message,
        IReadOnlyDictionary<string, string[]> errors) : base(message)
    { Code = code; Errors = errors; }
}
public sealed class DatabaseUnavailableException(string message, Exception? inner = null) : Exception(message, inner);