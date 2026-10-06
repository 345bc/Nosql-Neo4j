using Nosql_Neo4j.Models;
namespace Nosql_Neo4j.Services;

// Chỉ dành cho chẩn đoán Development; trang công khai dùng dữ liệu Published.
public interface IShapeDiagnosticService
{
    Task<IReadOnlyList<ShapeSummary>> GetDraftAsync();
    Task<ShapeDetail?> GetDraftByIdAsync(string id);
    Task<ShapeGraph> GetDraftGraphAsync(string fromId, string toId);
}
