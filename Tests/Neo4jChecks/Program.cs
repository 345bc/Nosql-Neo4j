using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Neo4j.Driver;
using Nosql_Neo4j.Configuration;
using Nosql_Neo4j.Models;
using Nosql_Neo4j.Repositories;
using Nosql_Neo4j.Services;
using System.Net;
using System.Text.RegularExpressions;
using System.Diagnostics;

if (!args.Contains("--run-local")) throw new InvalidOperationException("Thêm --run-local để kiểm tra database local bằng tài khoản/lượt tạm riêng.");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    { EnvironmentName = "Development", ContentRootPath = Directory.GetCurrentDirectory() });
builder.AddLocalNeo4jEnv();
await using var driver = GraphDatabase.Driver(builder.Configuration["Neo4j:Uri"],
    AuthTokens.Basic(builder.Configuration["Neo4j:Username"], builder.Configuration["Neo4j:Password"]));
var users = new UserRepository(driver, builder.Configuration);
var repository = new PracticeRepository(driver, builder.Configuration);
var reports = new PracticeReportRepository(driver, builder.Configuration);
var practice = new PracticeService(repository, TimeProvider.System, builder.Environment);
var hasher = new PasswordHasher<UserAccount>();
var accounts = new AccountService(users, hasher);
var ids = new[] { Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N") };
var count = 0;
var webIndex = Array.IndexOf(args, "--base-url");
HttpClient? web = null;
HttpClient? otherWeb = null;
void Check(bool condition, string message) { if (!condition) throw new Exception("FAIL: " + message); count++; }
try
{
    foreach (var userId in ids)
    {
        var account = new UserAccount(userId, "check_" + userId, "Kiểm tra tạm", "", "USER", "ACTIVE", Guid.NewGuid().ToString("N"));
        await users.CreateAsync(account with { PasswordHash = hasher.HashPassword(account, "TemporaryCheck@2026!") });
    }
    var shapes = new ShapeRepository(driver, builder.Configuration);
    var graphData = await shapes.GetDraftGraphAsync("HINH_VUONG", "TU_GIAC");
    Check(graphData.Nodes.Count == 6 && graphData.Edges.Count == 6, "core six shapes and direct edges");
    Check(graphData.Paths.Count == 2 && graphData.Paths.All(p => p.First() == "HINH_VUONG" && p.Last() == "TU_GIAC") &&
        graphData.Paths.Any(p => p.Contains("HINH_THOI")) && graphData.Paths.Any(p => p.Contains("HINH_CHU_NHAT")), "core two directed classification paths");
    var details = await Task.WhenAll(graphData.Nodes.Select(n => shapes.GetDraftByIdAsync(n.Id)));
    Check(details.All(d => d is { Definition.Length: > 0, Properties.Length: > 0, Examples.Length: > 0 }) &&
        details.Sum(d => d!.Formulas.Count) == 10, "core knowledge arrays and ten formulas");
    var pair = await shapes.GetDraftPairAsync("HINH_CHU_NHAT", "HINH_THOI");
    Check(pair is { Left.Id: "HINH_CHU_NHAT", Right.Id: "HINH_THOI" }, "core comparison pair");
    Check(await shapes.GetPublishedByIdAsync("HINH_VUONG") is null, "core excludes draft detail from published reader");
    foreach (var topic in new[] { "TU_GIAC", "HINH_THANG", "HINH_BINH_HANH", "HINH_CHU_NHAT", "HINH_THOI", "HINH_VUONG" })
        Check((await repository.GetDraftCandidatesAsync(topic)).Count >= 10, "ten draft candidates " + topic);
    var id = await practice.CreateDemoAsync(ids[0], "HINH_VUONG");
    var state = (await repository.ReadAsync(id, ids[0]))!;
    var input = new PracticeSubmitInput { Items = state.Questions.Select((q, i) => new PracticeAnswerInput
        { ItemId = q.Question.ItemId, SelectedKey = i < 7 ? q.CorrectKey : i < 9 ? (q.CorrectKey == "A" ? "B" : "A") : null }).ToList() };
    var results = await Task.WhenAll(practice.SubmitAsync(ids[0], id, input), practice.SubmitAsync(ids[0], id, input));
    Check(results.All(r => r?.Score == 7 && r.IncorrectCount == 2 && r.UnansweredCount == 1) &&
        results[0]!.SubmittedAt == results[1]!.SubmittedAt, "concurrent identical submissions are idempotent");
    Check(await practice.GetResultAsync(ids[1], id) is null && await practice.SubmitAsync(ids[1], id, input) is null, "database owner guards");
    var racedId = await practice.CreateDemoAsync(ids[0], "HINH_THANG");
    var raced = (await repository.ReadAsync(racedId, ids[0]))!;
    var allCorrect = new PracticeSubmitInput { Items = raced.Questions.Select(q => new PracticeAnswerInput
        { ItemId = q.Question.ItemId, SelectedKey = q.CorrectKey }).ToList() };
    async Task<string> SubmitRace(PracticeSubmitInput answers)
    {
        try { await practice.SubmitAsync(ids[0], racedId, answers); return "OK"; }
        catch (PracticeException e) { return e.Code; }
    }
    var race = await Task.WhenAll(SubmitRace(allCorrect), SubmitRace(new()));
    Check(race.Count(r => r == "OK") == 1 && race.Count(r => r == "ATTEMPT_ALREADY_SUBMITTED") == 1, "different concurrent submissions have one winner");
    for (var i = 0; i < 10; i++)
        await practice.SubmitAsync(ids[0], await practice.CreateDemoAsync(ids[0], "HINH_VUONG"), new());
    var history = await reports.HistoryAsync(ids[0], new() { Demo = true });
    Check(history.Total == 12 && history.Rows.Count == 10 && history.Rows.Select(r => r.Id).Distinct().Count() == 10, "history pagination first page");
    var next = await reports.HistoryAsync(ids[0], new() { Demo = true, Page = 2 });
    Check(next.Rows.Count == 2 && !history.Rows.Select(r => r.Id).Intersect(next.Rows.Select(r => r.Id)).Any(), "history pagination stable second page");
    Check((await reports.HistoryAsync(ids[1], new() { Demo = true })).Total == 0, "history isolates users");
    Check((await reports.HistoryAsync(ids[0], new())).Total == 0, "history excludes demos from official results");
    var stats = await reports.StatisticsAsync(ids[0], new() { Demo = true, TopicId = "HINH_VUONG" });
    Check(stats.Attempts == 11 && stats.Average == 0.64m && stats.Topics.Single().Total == 110 &&
        stats.Topics.Single().Correct == 7 && stats.Topics.Single().Percent == 6.4m, "statistics include blank items and correct rounding");
    Check((await reports.StatisticsAsync(ids[0], new())).Average is null, "official no-data average");
    var day = DateOnly.FromDateTime(state.StartedAt.ToOffset(TimeSpan.FromHours(7)).DateTime);
    Check((await reports.HistoryAsync(ids[0], new() { Demo = true, From = day, To = day })).Total == 12, "Vietnam date filter includes submissions");
    Check((await reports.HistoryAsync(ids[0], new() { Demo = true, To = day.AddDays(-1) })).Total == 0, "date filter excludes later submissions");
    if (webIndex >= 0)
    {
        var address = new Uri(args[webIndex + 1]);
        if (address.Scheme != "https" || !address.IsLoopback) throw new ArgumentException("HTTP checks only target local HTTPS.");
        HttpClient Client() => new(new HttpClientHandler { AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true }) { BaseAddress = address };
        static string Token(string html) => WebUtility.HtmlDecode(Regex.Match(html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        async Task Login(HttpClient client, string userId)
        {
            var login = await client.GetStringAsync("/Account/Login");
            var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string,string>
                { ["Username"] = "check_" + userId, ["Password"] = "TemporaryCheck@2026!", ["__RequestVerificationToken"] = Token(login) }));
            Check(response.StatusCode == HttpStatusCode.Redirect, "HTTP login");
        }
        web = Client(); otherWeb = Client();
        await Login(web, ids[0]); await Login(otherWeb, ids[1]);
        var index = await web.GetStringAsync("/Practice");
        var rejected = await web.PostAsync("/Practice/Demo", new FormUrlEncodedContent(new Dictionary<string,string> { ["topicId"] = "HINH_VUONG" }));
        Check(rejected.StatusCode == HttpStatusCode.BadRequest, "HTTP CSRF rejection");
        var created = await web.PostAsync("/Practice/Demo", new FormUrlEncodedContent(new Dictionary<string,string>
            { ["topicId"] = "HINH_CHU_NHAT", ["__RequestVerificationToken"] = Token(index) }));
        Check(created.StatusCode == HttpStatusCode.Redirect, "HTTP create demo");
        var takeUrl = created.Headers.Location!.ToString();
        var webId = takeUrl.Split('/').Last();
        var paperHtml = await web.GetStringAsync(takeUrl);
        Check(Regex.Matches(paperHtml, "name=\"Items\\[\\d+\\]\\.ItemId\"").Count == 10 &&
            !paperHtml.Contains("CorrectKey") && !paperHtml.Contains("Explanation"), "HTTP ten questions without answer leakage");
        Check((await otherWeb.GetAsync(takeUrl)).StatusCode == HttpStatusCode.NotFound, "HTTP other owner cannot read paper");
        var webState = (await repository.ReadAsync(webId, ids[0]))!;
        var form = new Dictionary<string,string> { ["__RequestVerificationToken"] = Token(paperHtml) };
        for (var i = 0; i < 10; i++)
        {
            form[$"Items[{i}].ItemId"] = webState.Questions[i].Question.ItemId;
            if (i < 7) form[$"Items[{i}].SelectedKey"] = webState.Questions[i].CorrectKey;
            else if (i < 9) form[$"Items[{i}].SelectedKey"] = webState.Questions[i].CorrectKey == "A" ? "B" : "A";
        }
        var submitted = await web.PostAsync("/Practice/Submit/" + webId, new FormUrlEncodedContent(form));
        Check(submitted.StatusCode == HttpStatusCode.Redirect, "HTTP submit");
        var resultHtml = await web.GetStringAsync(submitted.Headers.Location!.ToString());
        Check(resultHtml.Contains("7/10"), "HTTP grading result 7/2/1");
        Check((await otherWeb.GetAsync("/Practice/Result/" + webId)).StatusCode == HttpStatusCode.NotFound, "HTTP other owner cannot read result");
        Check((await web.GetAsync("/Progress/History?Demo=true")).StatusCode == HttpStatusCode.OK &&
            (await web.GetAsync("/Progress/Statistics?Demo=true")).StatusCode == HttpStatusCode.OK, "HTTP reports render");
        Check((await web.GetAsync("/Progress/History?From=2026-10-08&To=2026-10-07")).StatusCode == HttpStatusCode.BadRequest,
            "HTTP invalid date filter rejected");
        Check((await web.GetAsync("/Account/ChangePassword")).StatusCode == HttpStatusCode.OK, "HTTP change-password form");
        var productionAddress = new UriBuilder(address) { Port = address.Port + 1 }.Uri;
        var processInfo = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Directory.GetCurrentDirectory() };
        foreach (var value in new[] { Path.GetFullPath("obj/local-check/Nosql-Neo4j.dll"), "--urls", productionAddress.ToString(),
            "--environment", "Production", "--Logging:EventLog:LogLevel:Default", "None" }) processInfo.ArgumentList.Add(value);
        foreach (var key in new[] { "Uri", "Username", "Password", "Database" })
            processInfo.Environment["Neo4j__" + key] = builder.Configuration["Neo4j:" + key];
        using var production = Process.Start(processInfo)!;
        var productionOutput = production.StandardOutput.ReadToEndAsync();
        var productionError = production.StandardError.ReadToEndAsync();
        try
        {
            using var productionWeb = Client(); productionWeb.BaseAddress = productionAddress;
            bool ready = false;
            for (int i = 0; i < 50 && !ready; i++)
            {
                if (production.HasExited) throw new Exception("Production check server exited before startup.");
                try { ready = (await productionWeb.GetAsync("/Account/Login")).IsSuccessStatusCode; }
                catch (HttpRequestException) { await Task.Delay(100); }
            }
            Check(ready, "Production HTTP startup");
            await Login(productionWeb, ids[0]);
            Check((await productionWeb.GetAsync("/Dev/Shapes")).StatusCode == HttpStatusCode.NotFound &&
                (await productionWeb.GetAsync("/Dev/Questions")).StatusCode == HttpStatusCode.NotFound, "Production blocks draft diagnostic pages");
            Check((await productionWeb.GetAsync(takeUrl)).StatusCode == HttpStatusCode.NotFound &&
                (await productionWeb.GetAsync("/Practice/Result/" + webId)).StatusCode == HttpStatusCode.NotFound, "Production hides demo paper/results");
            var productionIndex = await productionWeb.GetStringAsync("/Practice");
            Check(!productionIndex.Contains("Chạy thử 10 câu"), "Production hides demo button");
            Check((await productionWeb.PostAsync("/Practice/Demo", new FormUrlEncodedContent(new Dictionary<string,string>
                { ["topicId"] = "HINH_VUONG", ["__RequestVerificationToken"] = Token(productionIndex) }))).StatusCode == HttpStatusCode.NotFound,
                "Production rejects demo creation");
            Check((await productionWeb.GetAsync("/Progress/History?Demo=true")).StatusCode == HttpStatusCode.NotFound,
                "Production hides demo reports");
        }
        finally
        {
            if (!production.HasExited) production.Kill();
            await production.WaitForExitAsync();
            await Task.WhenAll(productionOutput, productionError);
        }
    }
    var before = (await users.FindByIdAsync(ids[0]))!;
    Check(await accounts.ChangePasswordAsync(ids[0], "TemporaryCheck@2026!", "TemporaryChanged@2026!"), "Neo4j password change");
    var after = (await users.FindByIdAsync(ids[0]))!;
    Check(before.SecurityStamp != after.SecurityStamp && await accounts.AuthenticateAsync(before.Username, "TemporaryCheck@2026!") is null
        && await accounts.AuthenticateAsync(before.Username, "TemporaryChanged@2026!") is not null, "Neo4j password and stamp rotated");
    if (web is not null)
    {
        var revoked = await web.GetAsync("/Progress/History");
        Check(revoked.StatusCode == HttpStatusCode.Redirect && revoked.Headers.Location!.ToString().Contains("/Account/Login"),
            "HTTP old cookie revoked after password change");
    }
    await using var session = driver.AsyncSession(c => c.WithDatabase(builder.Configuration.GetNeo4jDatabaseName()));
    var graph = await session.RunAsync("MATCH (a:Attempt {id:$id})-[:HAS_ITEM]->(i:AttemptItem) RETURN count(i) AS items, sum(CASE WHEN i.isCorrect THEN 1 ELSE 0 END) AS score", new { id });
    await graph.FetchAsync();
    Check(graph.Current["items"].As<int>() == 10 && graph.Current["score"].As<int>() == 7, "graph snapshot and score atomic");
    Console.WriteLine($"PASS: {count} Neo4j integration checks.");
}
finally
{
    web?.Dispose(); otherWeb?.Dispose();
    // Delete only nodes owned by the two unique IDs created by this run.
    await using var session = driver.AsyncSession(c => c.WithDatabase(builder.Configuration.GetNeo4jDatabaseName()));
    await session.ExecuteWriteAsync(async tx =>
    {
        foreach (var query in new[] {
            "MATCH (u:User)-[:STARTED]->(:Attempt)-[:HAS_ITEM]->(i:AttemptItem) WHERE u.id IN $ids DETACH DELETE i",
            "MATCH (u:User)-[:STARTED]->(a:Attempt) WHERE u.id IN $ids DETACH DELETE a",
            "MATCH (u:User)-[:PERFORMED]->(e:AuditEvent) WHERE u.id IN $ids DETACH DELETE e",
            "MATCH (u:User) WHERE u.id IN $ids DETACH DELETE u" })
            await (await tx.RunAsync(query, new { ids })).ConsumeAsync();
    });
    Console.WriteLine("Đã dọn dữ liệu kiểm thử tạm; giữ nguyên tài khoản/câu hỏi/lượt có sẵn.");
}
