using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Neo4j.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nosql_Neo4j.Controllers
{
    public class ShapeDetails
    {
        public string? Name { get; set; }
        public List<string> Definitions { get; set; } = new();
        public List<string> Properties { get; set; } = new();
        public List<string> Signs { get; set; } = new();
        public List<string> Formulas { get; set; } = new();
    }

    public class CompareController : Controller
    {
        private readonly IDriver _driver;
        private readonly string _database;


        public CompareController(IDriver driver, IConfiguration configuration) 
        { 
            _driver = driver;
            _database = configuration["Neo4j:Database"]
        ?? throw new InvalidOperationException(
            "Chưa cấu hình Neo4j:Database.");
        }

        public async Task<IActionResult> Index()
        {
            await using var session = _driver.AsyncSession(configBuilder => configBuilder.WithDatabase(_database));
            var shapes = await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync("MATCH (s:Shape) RETURN s.id AS id, s.name AS name ORDER BY s.name");
                var items = new List<SelectListItem>();
                while (await cursor.FetchAsync())
                {
                    items.Add(new SelectListItem { Value = cursor.Current["id"].As<string>(), Text = cursor.Current["name"].As<string>() });
                }
                return items;
            });
            ViewBag.Shapes = shapes;
            return View();
        }

        public async Task<IActionResult> Result(string shape1, string shape2)
        {
            if (string.IsNullOrEmpty(shape1) || string.IsNullOrEmpty(shape2)) return RedirectToAction("Index");

            await using var session = _driver.AsyncSession(configBuilder => configBuilder.WithDatabase(_database));
            
            async Task<ShapeDetails> GetShapeInfo(string shapeId)
            {
                return await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(@"
                        MATCH (s:Shape {id: $id})-[:IS_A*0..]->(parent:Shape)
                        OPTIONAL MATCH (parent)-[:HAS_FORMULA]->(f:Formula)
                        RETURN s.name AS Name,
                               collect(DISTINCT parent.definition) AS Definitions,
                               collect(DISTINCT parent.properties) AS PropertiesList,
                               collect(DISTINCT parent.recognitionSigns) AS SignsList,
                               collect(DISTINCT CASE WHEN f IS NOT NULL THEN f.name + ': ' + f.expression ELSE null END) AS Formulas
                    ", new { id = shapeId });

                    var details = new ShapeDetails { Name = shapeId };
                    if (await cursor.FetchAsync())
                    {
                        var current = cursor.Current;
                        details.Name = current["Name"].As<string>();

                        if (current["Definitions"].As<IList<object>>() is var defs && defs != null)
                            details.Definitions = defs.Select(x => x?.ToString() ?? "").ToList();

                        if (current["PropertiesList"].As<IList<object>>() is var pList && pList != null)
                            foreach (IList<object> arr in pList.OfType<IList<object>>())
                                details.Properties.AddRange(arr.Select(x => x?.ToString() ?? ""));

                        if (current["SignsList"].As<IList<object>>() is var sList && sList != null)
                            foreach (IList<object> arr in sList.OfType<IList<object>>())
                                details.Signs.AddRange(arr.Select(x => x?.ToString() ?? ""));

                        if (current["Formulas"].As<IList<object>>() is var fList && fList != null)
                            details.Formulas = fList.Select(x => x?.ToString() ?? "").ToList();
                        
                        // Lọc trùng lặp do kế thừa
                        details.Properties = details.Properties.Distinct().ToList();
                        details.Signs = details.Signs.Distinct().ToList();
                    }
                    return details;
                });
            }

            ViewBag.Shape1 = await GetShapeInfo(shape1);
            ViewBag.Shape2 = await GetShapeInfo(shape2);
            return View();
        }
    }
}