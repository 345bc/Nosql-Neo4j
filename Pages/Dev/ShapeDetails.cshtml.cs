using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Neo4j.Driver;
using Nosql_Neo4j.Models;
using Nosql_Neo4j.Repositories;

namespace Nosql_Neo4j.Pages.Dev;

public class ShapeDetailsModel : PageModel
{
    private readonly IShapeRepository _repository;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ShapeDetailsModel> _logger;

    public ShapeDetailsModel(IShapeRepository repository, IWebHostEnvironment environment,
        ILogger<ShapeDetailsModel> logger)
    {
        _repository = repository;
        _environment = environment;
        _logger = logger;
    }

    public ShapeDetail? Shape { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!_environment.IsDevelopment()) return NotFound();
        Response.Headers.CacheControl = "no-store";
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        try
        {
            Shape = await _repository.GetDraftByIdAsync(id);
            if (Shape is null) return NotFound();
        }
        catch (Neo4jException exception)
        {
            _logger.LogError("Không đọc được chi tiết hình. Loại lỗi: {ErrorType}; requestId: {RequestId}",
                exception.GetType().Name, HttpContext.TraceIdentifier);
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            ErrorMessage = "Không đọc được dữ liệu Neo4j. Kiểm tra instance và database nosql-neo4j.";
        }

        return Page();
    }
}
