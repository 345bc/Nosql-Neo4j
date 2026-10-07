using Neo4j.Driver;
using Microsoft.Extensions.Options;
using Nosql_Neo4j.Configuration;
using Nosql_Neo4j.Contracts;
using Nosql_Neo4j.Data;
using Nosql_Neo4j.Repositories;
using Nosql_Neo4j.Services;
var builder = WebApplication.CreateBuilder(args);
builder.AddLocalNeo4jEnv();
builder.Services.AddControllersWithViews(options => options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
builder.Services.Configure<Neo4jOptions>(builder.Configuration.GetSection("Neo4j"));
builder.Services.AddSingleton<IDriver>(sp =>
{
    var o = sp.GetRequiredService<IOptions<Neo4jOptions>>().Value;
    if (string.IsNullOrWhiteSpace(o.Password) || o.Password == "CHANGE_ME") throw new DatabaseUnavailableException("Chưa cấu hình Neo4j.");
    return GraphDatabase.Driver(o.Uri, AuthTokens.Basic(o.Username, o.Password), c => c.WithConnectionTimeout(TimeSpan.FromSeconds(5)).WithMaxTransactionRetryTime(TimeSpan.FromSeconds(5)));
});
builder.Services.AddSingleton<Neo4jConnection>(sp => new(sp.GetRequiredService<IOptions<Neo4jOptions>>(), () => sp.GetRequiredService<IDriver>()));
builder.Services.AddScoped<IShapeRepository, ShapeRepository>();
builder.Services.AddScoped<IShapeDiagnosticService, ShapeDiagnosticService>();
builder.Services.AddScoped<IKnowledgeSnapshotRepository, Neo4jShapeRepository>();
builder.Services.AddScoped<IShapeService, ShapeService>();
var app = builder.Build();
if (args.Contains("--seed-demo"))
{
    await using var scope = app.Services.CreateAsyncScope();
    try
    {
        await DemoSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<Neo4jConnection>(), app.Environment.ContentRootPath);
    }
    catch (Exception ex) when (ex is Neo4jException or DatabaseUnavailableException or InvalidOperationException)
    {
        // A failed seed must not dump credentials or driver stack traces to the console.
        Console.Error.WriteLine("Không thể seed demo. Kiểm tra instance đang chạy, mật khẩu, tên database đã tồn tại và database dành riêng cho demo. Nếu đã có dữ liệu khác, chọn database trống mới.");
        Environment.ExitCode = 1;
    }
    await app.DisposeAsync(); return;
}
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Error"); app.UseHsts(); app.UseHttpsRedirection(); }
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "same-origin";
    // Scope the policy to the new MVC knowledge views; other team views keep their behavior.
    if (context.Request.Path == "/" || context.Request.Path.Equals("/Home/Index", StringComparison.OrdinalIgnoreCase) || context.Request.Path.StartsWithSegments("/Shapes") || context.Request.Path.StartsWithSegments("/Status"))
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'";
    await next();
});
app.UseStatusCodePagesWithReExecute("/Status", "?code={0}");
app.UseRouting(); app.UseAuthorization(); app.MapStaticAssets();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.Run();
public partial class Program;