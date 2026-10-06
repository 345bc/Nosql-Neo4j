using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Neo4j.Driver;
using Nosql_Neo4j.Models;
using Nosql_Neo4j.Repositories;

namespace Nosql_Neo4j.Pages.Dev;

public class GraphModel(IShapeRepository repository, IWebHostEnvironment environment,
    ILogger<GraphModel> logger) : PageModel
{
    public ShapeGraph? Graph { get; private set; }
    public string? ErrorMessage { get; private set; }

    public string NameOf(string id)
        => Graph?.Nodes.FirstOrDefault(n => n.Id == id)?.Name ?? id;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!environment.IsDevelopment()) return NotFound();
        Response.Headers.CacheControl = "no-store";
        try
        {
            Graph = await repository.GetDraftGraphAsync("HINH_VUONG", "TU_GIAC");
        }
        catch (Neo4jException exception)
        {
            logger.LogError("Không đọc được đồ thị. Loại lỗi: {ErrorType}; requestId: {RequestId}",
                exception.GetType().Name, HttpContext.TraceIdentifier);
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            ErrorMessage = "Không đọc được đồ thị Neo4j. Kiểm tra instance và database nosql-neo4j.";
        }
        return Page();
    }
}
