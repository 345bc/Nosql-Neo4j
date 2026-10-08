using Nosql_Neo4j.Contracts;
using Nosql_Neo4j.Models.Data;
using Nosql_Neo4j.Services;
using Xunit;
namespace Shapes.Tests;

public sealed class ShapeServiceTests
{
    private static ShapeService Service(KnowledgeSnapshot? snapshot = null) => new(new MemoryRepository(snapshot ?? TestData.Snapshot()));
    [Theory]
    [InlineData(" HÌNH   CHỮ NHẬT ", "hinh chu nhat")]
    [InlineData("Đường chéo", "duong cheo")]
    [InlineData("HÌNH THOI", "hinh thoi")]
    public void NormalizeVietnamese(string input, string expected) => Assert.Equal(expected, VietnameseSearch.Normalize(input));
    [Theory]
    [InlineData("hinh thoi", "HINH_THOI")]
    [InlineData(" HÌNH  THOI ", "HINH_THOI")]
    [InlineData("rhombus", "HINH_THOI")]
    public async Task SearchesNamesAndAliases(string input, string id) { var result = await Service().ListAsync(input, 1, default); Assert.Equal(id, Assert.Single(result.Items).Id); }
    [Fact] public async Task EmptySearchListsSix() { Assert.Equal(6, (await Service().ListAsync(null, 1, default)).TotalItems); }
    [Fact] public async Task UnknownSearchIsEmpty() { Assert.Empty((await Service().ListAsync("không có", 1, default)).Items); }
    [Fact]
    public async Task ExactThenPrefixThenContains()
    {
        var shapes = new[] { new ShapeRecord("A", "zz hình thoi", [], null), new ShapeRecord("B", "hình thoi đặc biệt", [], null), new ShapeRecord("C", "hình thoi", [], null) };
        var result = await Service(new(shapes, [], [])).ListAsync("hinh thoi", 1, default); Assert.Equal(new[] { "C", "B", "A" }, result.Items.Select(s => s.Id));
    }
    [Theory][InlineData(0)][InlineData(-1)] public async Task InvalidPage(int page) { await Assert.ThrowsAsync<DomainValidationException>(() => Service().ListAsync(null, page, default)); }
    [Fact] public async Task LongSearchRejected() { await Assert.ThrowsAsync<DomainValidationException>(() => Service().ListAsync(new string('a', 101), 1, default)); }
    [Fact]
    public async Task PagingTwentyAndExtremePageDoesNotOverflow()
    {
        var shapes = Enumerable.Range(1, 21).Select(i => new ShapeRecord($"S{i:00}", $"Hình {i:00}", [], null)).ToArray(); var svc = Service(new(shapes, [], []));
        Assert.Equal(20, (await svc.ListAsync(null, 1, default)).Items.Length); Assert.Single((await svc.ListAsync(null, 2, default)).Items); Assert.Empty((await svc.ListAsync(null, int.MaxValue, default)).Items);
    }
    [Fact]
    public async Task SquareHasFiveGroupsAndInheritedProperties()
    {
        var detail = await Service().GetDetailAsync("HINH_VUONG", default); Assert.NotNull(detail);
        Assert.NotEmpty(detail.Definition); Assert.Equal(15, detail.Properties.Length); Assert.NotEmpty(detail.RecognitionSigns); Assert.Equal(3, detail.Formulas.Length); Assert.NotEmpty(detail.Examples); Assert.NotEmpty(detail.Sources);
        Assert.Contains(detail.Properties, p => p.Contains("Kế thừa từ")); Assert.All(detail.Formulas, f => Assert.NotEmpty(f.Conditions));
    }
    [Fact] public async Task MissingDetailReturnsNull() { Assert.Null(await Service().GetDetailAsync("NOT_FOUND", default)); }
    [Fact]
    public async Task DiamondHasExactlyTwoDirectedPaths()
    {
        var graph = await Service().GetGraphAsync("HINH_VUONG", "TU_GIAC", default); Assert.Equal(6, graph.Nodes.Length); Assert.Equal(6, graph.Edges.Length); Assert.Equal(2, graph.Paths.Length);
        Assert.Contains(graph.Paths, p => p.Contains("HINH_CHU_NHAT")); Assert.Contains(graph.Paths, p => p.Contains("HINH_THOI")); Assert.All(graph.Paths, p => { Assert.Equal("HINH_VUONG", p[0]); Assert.Equal("TU_GIAC", p[^1]); Assert.Equal(5, p.Length); });
    }
    [Fact] public async Task ReverseDirectionHasNoPath() { Assert.Empty((await Service().GetGraphAsync("TU_GIAC", "HINH_VUONG", default)).Paths); }
    [Fact] public async Task IdentityPathHasZeroEdges() { Assert.Equal(new[] { "HINH_VUONG" }, Assert.Single((await Service().GetGraphAsync("HINH_VUONG", "HINH_VUONG", default)).Paths)); }
    [Theory]
    [InlineData("HINH_VUONG", null)]
    [InlineData(null, "TU_GIAC")]
    [InlineData("BAD", "TU_GIAC")]
    public async Task InvalidPathInputs(string? from, string? to) { await Assert.ThrowsAsync<DomainValidationException>(() => Service().GetGraphAsync(from, to, default)); }
    [Theory]
    [InlineData("TU_GIAC", "HINH_VUONG")]
    [InlineData("TU_GIAC", "TU_GIAC")]
    public async Task RejectsCycleAndSelfEdge(string child, string parent)
    { var s = TestData.Snapshot(); s = s with { Edges = [.. s.Edges, new(child, parent)] }; await Assert.ThrowsAsync<DomainValidationException>(() => Service(s).GetGraphAsync(null, null, default)); }
    [Fact]
    public async Task RejectsOverHundredNodes()
    { var s = new KnowledgeSnapshot(Enumerable.Range(0, 101).Select(i => new ShapeRecord(i.ToString(), "Hình", [], null)).ToArray(), [], []); await Assert.ThrowsAsync<DomainValidationException>(() => Service(s).GetGraphAsync(null, null, default)); }
    [Fact]
    public async Task HiddenAncestorIsNotInherited()
    { var s = TestData.Snapshot(); s = s with { Shapes = s.Shapes.Where(n => n.Id != "HINH_THOI").ToArray(), Knowledge = s.Knowledge.Where(k => k.ShapeId != "HINH_THOI").ToArray(), Edges = s.Edges.Where(e => e.SourceId != "HINH_THOI" && e.TargetId != "HINH_THOI").ToArray() }; var d = await Service(s).GetDetailAsync("HINH_VUONG", default); Assert.DoesNotContain(d!.Properties, p => p.Contains("Hai đường chéo vuông góc")); }

    [Fact]
    public async Task DepthTenAllowedElevenRejected()
    {
        KnowledgeSnapshot Chain(int edges) => new(Enumerable.Range(0, edges + 1).Select(i => new ShapeRecord($"S{i}", "Hình", [], null)).ToArray(), [], Enumerable.Range(0, edges).Select(i => new EdgeRecord($"S{i}", $"S{i + 1}")).ToArray());
        Assert.Single((await Service(Chain(10)).GetGraphAsync("S0", "S10", default)).Paths);
        await Assert.ThrowsAsync<DomainValidationException>(() => Service(Chain(11)).GetGraphAsync(null, null, default));
    }
    [Theory]
    [InlineData("HinhVuong", "HINH_VUONG")]
    [InlineData("TuGiac", "TU_GIAC")]
    [InlineData("OTHER", "OTHER")]
    public void StableLegacyIds(string legacy, string expected) => Assert.Equal(expected, Nosql_Neo4j.Repositories.ShapeIds.Canonical(legacy));
}