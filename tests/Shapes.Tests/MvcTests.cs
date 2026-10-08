using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nosql_Neo4j.Contracts;
using Nosql_Neo4j.Repositories;
using Xunit;
namespace Shapes.Tests;

public sealed class AppFactory(IKnowledgeSnapshotRepository repository) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development").ConfigureServices(services => { services.RemoveAll<IKnowledgeSnapshotRepository>(); services.AddSingleton<IKnowledgeSnapshotRepository>(repository); });
}
public sealed class MvcTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/Home/Index")]
    [InlineData("/Shapes")]
    [InlineData("/Shapes/Index")]
    [InlineData("/Shapes/Details/HINH_THOI")]
    [InlineData("/Shapes/Graph")]
    public async Task PublicPagesWork(string url) { await using var app = new AppFactory(new MemoryRepository(TestData.Snapshot())); using var client = app.CreateClient(); var response = await client.GetAsync(url); Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Contains("text/html", response.Content.Headers.ContentType!.ToString()); }
    [Fact] public async Task SearchRendersExpectedShape() { await using var app = new AppFactory(new MemoryRepository(TestData.Snapshot())); using var client = app.CreateClient(); var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Shapes?q=hinh%20thoi")); Assert.Contains("Hình thoi", html); Assert.DoesNotContain("/Shapes/Details/HINH_VUONG", html); }
    [Fact] public async Task BothPathsRenderInText() { await using var app = new AppFactory(new MemoryRepository(TestData.Snapshot())); using var client = app.CreateClient(); var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Shapes/Graph?fromId=HINH_VUONG&toId=TU_GIAC")); Assert.Contains("2 đường phân loại", html); Assert.Contains("Hình vuông → Hình chữ nhật → Hình bình hành → Hình thang → Tứ giác", html); Assert.Contains("Hình vuông → Hình thoi → Hình bình hành → Hình thang → Tứ giác", html); Assert.Contains("Quan hệ dạng văn bản", html); }
    [Theory]
    [InlineData("/Shapes?page=0")]
    [InlineData("/Shapes?page=x")]
    [InlineData("/Shapes/Graph?fromId=HINH_VUONG")]
    public async Task BadInputsReturn400(string url) { await using var app = new AppFactory(new MemoryRepository(TestData.Snapshot())); using var client = app.CreateClient(); Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(url)).StatusCode); }
    [Fact] public async Task MissingShape404() { await using var app = new AppFactory(new MemoryRepository(TestData.Snapshot())); using var client = app.CreateClient(); Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Shapes/Details/NOPE")).StatusCode); }
    [Fact]
    public async Task HtmlIsEncoded()
    { var s = TestData.Snapshot(); s = s with { Shapes = s.Shapes.Select(n => n.Id == "HINH_THOI" ? n with { Name = "<script>alert(1)</script>" } : n).ToArray() }; await using var app = new AppFactory(new MemoryRepository(s)); using var client = app.CreateClient(); var html = await client.GetStringAsync("/Shapes"); Assert.DoesNotContain("<script>alert(1)</script>", html); Assert.Contains("&lt;script&gt;", html); }
    [Theory]
    [InlineData("/Shapes")]
    [InlineData("/Shapes/Details/HINH_THOI")]
    [InlineData("/Shapes/Graph")]
    public async Task UnavailableDatabaseHas503AndNoSecret(string url)
    { await using var app = new AppFactory(new FailingRepository()); using var client = app.CreateClient(); var response = await client.GetAsync(url); Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); var html = await response.Content.ReadAsStringAsync(); Assert.DoesNotContain("private-secret", html); Assert.Contains("Thử lại", WebUtility.HtmlDecode(html)); }
    [Fact]
    public async Task SecurityHeaderAndNoRestEndpoint()
    { await using var app = new AppFactory(new MemoryRepository(TestData.Snapshot())); using var client = app.CreateClient(); var response = await client.GetAsync("/Shapes"); Assert.Contains("script-src 'self'", response.Headers.GetValues("Content-Security-Policy").Single()); Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/shapes")).StatusCode); }

    [Fact]
    public async Task PagingLinksUseMvcQueryAndKeepSearchTerm()
    {
        var snapshot = new Nosql_Neo4j.Models.Data.KnowledgeSnapshot(Enumerable.Range(1, 21).Select(i => new Nosql_Neo4j.Models.Data.ShapeRecord($"S{i:00}", $"Hình {i:00}", [], null)).ToArray(), [], []);
        await using var app = new AppFactory(new MemoryRepository(snapshot)); using var client = app.CreateClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Shapes?q=Hinh")); Assert.Contains("page=2", html);
        Assert.Contains("q=Hinh", html);
        var second = WebUtility.HtmlDecode(await client.GetStringAsync("/Shapes?q=Hinh&page=2")); Assert.Contains("Hình 21", second); Assert.DoesNotContain("/Shapes/Details/S01", second);
    }

    [Fact]
    public async Task ShapesRoutesAreMvcActionsAndNoPageEndpointsExist()
    {
        await using var app = new AppFactory(new MemoryRepository(TestData.Snapshot())); using var client = app.CreateClient();
        await client.GetAsync("/Shapes");
        var endpoints = app.Services.GetRequiredService<EndpointDataSource>().Endpoints;
        var actions = endpoints.Select(e => e.Metadata.GetMetadata<ControllerActionDescriptor>())
            .OfType<ControllerActionDescriptor>().Where(a => a.ControllerName == "Shapes").ToArray();
        Assert.Equal(new[] { "Details", "Graph", "Index" }, actions.Select(a => a.ActionName).Distinct().Order().ToArray());
        Assert.DoesNotContain(endpoints, e => e.Metadata.Any(m => m.GetType().Name == "PageActionDescriptor"));
        var html = await client.GetStringAsync("/Shapes");
        Assert.DoesNotContain("asp-page", html); Assert.DoesNotContain("asp-controller", html);
    }

    [Theory]
    [InlineData("/Shapes")]
    [InlineData("/Shapes/Details/HINH_THOI")]
    [InlineData("/Shapes/Graph")]
    public async Task ReadActionsRejectPost(string url)
    {
        await using var app = new AppFactory(new MemoryRepository(TestData.Snapshot())); using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.PostAsync(url, new StringContent(""))).StatusCode);
    }

    [Fact]
    public async Task EmptyAndUnknownSearchShowEmptyState()
    {
        await using var app = new AppFactory(new MemoryRepository(TestData.Snapshot())); using var client = app.CreateClient();
        var response = await client.GetAsync("/Shapes?q=khongcotennao"); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Không tìm thấy hình phù hợp", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        await using var emptyApp = new AppFactory(new MemoryRepository(new([], [], []))); using var emptyClient = emptyApp.CreateClient();
        Assert.Contains("Chưa có hình được công bố", WebUtility.HtmlDecode(await emptyClient.GetStringAsync("/Shapes/Graph")));
    }

    [Fact]
    public async Task LongQueryIsRejectedBeforeReadingDatabase()
    {
        await using var app = new AppFactory(new FailingRepository()); using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/Shapes?q=" + new string('a', 101))).StatusCode);
    }

    [Fact]
    public async Task InvalidGraphPairKeepsSelectorsAndCycleDoesNotRenderSvg()
    {
        await using var app = new AppFactory(new MemoryRepository(TestData.Snapshot())); using var client = app.CreateClient();
        var response = await client.GetAsync("/Shapes/Graph?fromId=NOPE&toId=TU_GIAC"); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("name=\"fromId\"", await response.Content.ReadAsStringAsync());
        var cyclic = TestData.Snapshot(); cyclic = cyclic with { Edges = [..cyclic.Edges, new("TU_GIAC", "HINH_VUONG")] };
        await using var cyclicApp = new AppFactory(new MemoryRepository(cyclic)); using var cyclicClient = cyclicApp.CreateClient();
        var invalid = await cyclicClient.GetAsync("/Shapes/Graph"); Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.DoesNotContain("class=\"taxonomy\"", await invalid.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SvgNodesAndNavigationUseWorkingMvcUrls()
    {
        await using var app = new AppFactory(new MemoryRepository(TestData.Snapshot())); using var client = app.CreateClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Shapes/Graph"));
        Assert.Contains("href=\"/Shapes/Details/HINH_VUONG\"", html);
        Assert.Contains("href=\"/Shapes\"", html); Assert.Contains("href=\"/Compare\"", html);
        Assert.Matches("<a[^>]*class=\"active\"[^>]*>Đồ thị phân loại", html);
    }
    private sealed class FailingRepository : IKnowledgeSnapshotRepository
    { public Task<Nosql_Neo4j.Models.Data.KnowledgeSnapshot> ReadPublishedAsync(CancellationToken ct) => throw new DatabaseUnavailableException("private-secret"); }
}