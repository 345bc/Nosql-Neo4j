using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Nosql_Neo4j.Models;
using Nosql_Neo4j.Repositories;
using Nosql_Neo4j.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Nosql_Neo4j.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;

var clock = new TestClock();
var repo = new FakeRepository();
var environment = new TestEnvironment();
var service = new PracticeService(repo, clock, environment);
var count = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    count++;
}
async Task Error(string code, Func<Task> action)
{
    try { await action(); throw new Exception("Expected " + code); }
    catch (PracticeException e) { Check(e.Code == code, code); }
}
var databaseConfig = new ConfigurationBuilder().AddInMemoryCollection().Build();
Check(databaseConfig.GetNeo4jDatabaseName() == "nosql-neo4j", "database default for existing configurations");
databaseConfig["Neo4j:Database"] = "integration-demo";
Check(databaseConfig.GetNeo4jDatabaseName() == "integration-demo", "configured database overrides default");
databaseConfig["Neo4j:Database"] = " ";
try
{
    databaseConfig.GetNeo4jDatabaseName();
    throw new Exception("Expected blank database rejection");
}
catch (InvalidOperationException)
{
    Check(true, "blank database rejected instead of selecting a different database");
}
var envRoot = Path.Combine(Path.GetTempPath(), "neo4j-database-check-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(envRoot);
try
{
    File.WriteAllText(Path.Combine(envRoot, ".env"), "Neo4j__Database=integration-demo\n");
    var envBuilder = WebApplication.CreateBuilder(new WebApplicationOptions
        { EnvironmentName = "Development", ContentRootPath = envRoot, Args = [] });
    envBuilder.AddLocalNeo4jEnv();
    Check(envBuilder.Configuration.GetNeo4jDatabaseName() ==
        (Environment.GetEnvironmentVariable("Neo4j__Database") ?? "integration-demo"),
        "local env accepts database key with environment override priority");
}
finally
{
    File.Delete(Path.Combine(envRoot, ".env"));
    Directory.Delete(envRoot);
}
await Error("INSUFFICIENT_QUESTIONS", async () => await service.CreateAsync("u1", null));
repo.Candidates = Enumerable.Range(1, 10).Select(i => new QuestionSnapshot(
    new PracticeQuestion("", $"q{i}", $"v{i}", "HINH_VUONG", $"Câu {i}",
        [new("A", "Một"), new("B", "Hai"), new("C", "Ba"), new("D", "Bốn")]),
    "C", "Giải thích")).ToArray();
var id = await service.CreateAsync("u1", "HINH_VUONG");
var paper = (await service.GetPaperAsync("u1", id))!;
Check(paper.Questions.Length == 10 && paper.Questions.Select(q => q.QuestionId).Distinct().Count() == 10, "unique 10");
var json = JsonSerializer.Serialize(paper);
Check(!json.Contains("CorrectKey") && !json.Contains("Explanation"), "paper hides answers");
Check(await service.GetPaperAsync("u2", id) is null, "paper ownership");
var input = new PracticeSubmitInput
{
    Items = paper.Questions.Select((q, i) => new PracticeAnswerInput
    { ItemId = q.ItemId, SelectedKey = i < 7 ? "C" : i < 9 ? "A" : null }).ToList()
};
await Error("VALIDATION_ERROR", async () => await service.SubmitAsync("u1", id,
    new PracticeSubmitInput { Items = [input.Items[0], input.Items[0]] }));
await Error("VALIDATION_ERROR", async () => await service.SubmitAsync("u1", id,
    new PracticeSubmitInput { Items = [new() { ItemId = "outside", SelectedKey = "C" }] }));
Check(await service.SubmitAsync("u2", id, input) is null, "submit ownership");
var result = (await service.SubmitAsync("u1", id, input))!;
Check(result.Score == 7 && result.IncorrectCount == 2 && result.UnansweredCount == 1, "7/2/1");
Check(result.Items.All(i => i.CorrectKey == "C" && i.Explanation.Length > 0), "result answers");
input.Items.Reverse();
var replay = (await service.SubmitAsync("u1", id, input))!;
Check(replay.Score == result.Score && replay.SubmittedAt == result.SubmittedAt, "replay same result");
input.Items[0].SelectedKey = "A";
await Error("ATTEMPT_ALREADY_SUBMITTED", async () => await service.SubmitAsync("u1", id, input));
var expiredId = await service.CreateAsync("u1", null);
clock.Now = clock.Now.AddHours(24);
await Error("ATTEMPT_EXPIRED", async () => await service.GetPaperAsync("u1", expiredId));
await Error("ATTEMPT_EXPIRED", async () => await service.SubmitAsync("u1", expiredId, new()));
Check(await service.GetResultAsync("u2", id) is null, "result ownership");
Check((await service.GetResultAsync("u1", id))!.Score == 7, "result retained after 24h");
clock.Now = clock.Now.AddDays(-1);
var blankId = await service.CreateAsync("u1", null);
var blank = (await service.SubmitAsync("u1", blankId, new()))!;
Check(blank.Score == 0 && blank.UnansweredCount == 10, "empty submission");
var user = new UserAccount("u", "tuan", "Tuấn", "", "USER", "ACTIVE", "s");
var hasher = new PasswordHasher<UserAccount>();
var hash = hasher.HashPassword(user, "a-long-test-password");
Check(hash != "a-long-test-password" && hasher.VerifyHashedPassword(user, hash, "wrong") == PasswordVerificationResult.Failed,
    "password hash verification");
var userRepo = new FakeUsers { User = user with { PasswordHash = hash } };
var accounts = new AccountService(userRepo, hasher);
Check(await accounts.AuthenticateAsync("tuan", "a-long-test-password") is not null, "valid login");
Check(await accounts.AuthenticateAsync("tuan", "wrong") is null, "wrong password");
Check(await accounts.AuthenticateAsync("missing", "a-long-test-password") is null, "unknown account");
userRepo.User = userRepo.User with { Status = "LOCKED" };
Check(await accounts.AuthenticateAsync("tuan", "a-long-test-password") is null, "locked account");
await Error("VALIDATION_ERROR", async () => await service.SubmitAsync("u1", blankId,
    new PracticeSubmitInput { Items = [new() { ItemId = (await service.GetResultAsync("u1", blankId))!.Items[0].Question.ItemId, SelectedKey = "Z" }] }));
var demoId = await service.CreateDemoAsync("u1", "HINH_VUONG");
Check((await service.GetPaperAsync("u1", demoId))!.IsDemo, "demo paper marked");
var demoResult = (await service.SubmitAsync("u1", demoId, new()))!;
Check(demoResult.IsDemo && demoResult.UnansweredCount == 10, "demo result marked");
environment.EnvironmentName = "Production";
await Error("DEMO_DISABLED", async () => await service.CreateDemoAsync("u1", null));
Check(await service.GetPaperAsync("u1", demoId) is null, "production hides demo paper");
Check(await service.GetResultAsync("u1", demoId) is null, "production hides demo result");
await Error("DEMO_DISABLED", async () => await service.SubmitAsync("u1", demoId, new()));
Check((await service.GetResultAsync("u1", id))!.Score == 7, "production retains published result");
var bounds = new PracticeReportFilter { From = new(2026, 10, 7), To = new(2026, 10, 7) }.Bounds();
Check(bounds.Start == new DateTimeOffset(2026, 10, 6, 17, 0, 0, TimeSpan.Zero) &&
    bounds.End == new DateTimeOffset(2026, 10, 7, 17, 0, 0, TimeSpan.Zero), "Vietnam inclusive day boundaries");
Check(new PracticeReportFilter().Bounds() == (null, null), "unbounded report dates");
try { new PracticeReportFilter { From = new(2026,10,8), To = new(2026,10,7) }.Bounds(); throw new Exception("reversed dates accepted"); }
catch (ArgumentException) { Check(true, "reject reversed report dates"); }
Check(new TopicAccuracy("topic", 7, 10).Percent == 70m && new TopicAccuracy("topic", 1, 3).Percent == 33.3m,
    "topic rate includes blanks and rounds one decimal");
userRepo.User = userRepo.User with { Status = "ACTIVE" };
var oldStamp = userRepo.User.SecurityStamp;
Check(!await accounts.ChangePasswordAsync("u", "wrong", "new-test-password-long"), "password change requires current password");
Check(!await accounts.ChangePasswordAsync("u", "a-long-test-password", "short"), "reject short new password");
Check(!await accounts.ChangePasswordAsync("u", "a-long-test-password", "a-long-test-password"), "reject unchanged password");
Check(await accounts.ChangePasswordAsync("u", "a-long-test-password", "new-test-password-long"), "password change succeeds");
Check(userRepo.User.SecurityStamp != oldStamp && await accounts.AuthenticateAsync("tuan", "a-long-test-password") is null
    && await accounts.AuthenticateAsync("tuan", "new-test-password-long") is not null, "stamp rotated and old password rejected");
Check(!await userRepo.ChangePasswordAsync("u", oldStamp, hash, "stale"), "stale concurrent password update rejected");
Check(LocalDataCommand.SplitStatements("// comment;\nRETURN 'a;b'; RETURN 2; // end").Count() == 2,
    "Cypher splitter preserves quoted semicolons");
var demoSeed = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "migration", "seed-all.cypher"));
var migrationQueries = LocalDataCommand.SplitStatements(demoSeed).ToArray();
var questionsQuery = migrationQueries.Single(q => q.Contains("MERGE (q:Question {id: row.id})"));
var questionIds = System.Text.RegularExpressions.Regex.Matches(questionsQuery, "(?<![A-Za-z])id: \"([^\"]+)\"")
    .Select(m => m.Groups[1].Value).ToArray();
var topicIds = System.Text.RegularExpressions.Regex.Matches(questionsQuery, "topicId: \"([^\"]+)\"")
    .Select(m => m.Groups[1].Value).ToArray();
Check(questionIds.Length == 60 && questionIds.Distinct().Count() == 60 &&
    topicIds.GroupBy(id => id).Count() == 6 && topicIds.GroupBy(id => id).All(g => g.Count() == 10),
    "consolidated seed contains 60 unique questions across six topics");
var verificationQueries = LocalDataCommand.ReadVerificationStatements(demoSeed);
Check(verificationQueries.Count > 0 && verificationQueries.All(q =>
    q.TrimStart().StartsWith("MATCH ", StringComparison.Ordinal) && !q.Contains("MERGE ") && !q.Contains("SET ")),
    "verification command selects read-only checks without replaying seed writes");
var demoHash = System.Text.RegularExpressions.Regex.Match(demoSeed, "u\\.passwordHash = '([^']+)'").Groups[1].Value;
Check(hasher.VerifyHashedPassword(user, demoHash, "DemoTuan@2026!") != PasswordVerificationResult.Failed,
    "seed demo password hash accepted by application Identity hasher");
Check(hasher.VerifyHashedPassword(user, demoHash, "wrong-password") == PasswordVerificationResult.Failed,
    "seed demo rejects incorrect password");
Console.WriteLine($"PASS: {count} checks.");

sealed class TestClock : TimeProvider
{
    public DateTimeOffset Now = new(2026, 10, 7, 0, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
}
sealed class FakeRepository : IPracticeRepository
{
    public IReadOnlyList<QuestionSnapshot> Candidates { get; set; } = [];
    private readonly Dictionary<string, AttemptState> states = [];
    public Task<IReadOnlyList<QuestionSnapshot>> GetCandidatesAsync(string? topicId) => Task.FromResult(Candidates);
    public Task<IReadOnlyList<QuestionSnapshot>> GetDraftCandidatesAsync(string? topicId) => Task.FromResult(Candidates);
    public Task CreateAsync(AttemptState attempt) { states.Add(attempt.Id, attempt); return Task.CompletedTask; }
    public Task<AttemptState?> ReadAsync(string id, string userId)
        => Task.FromResult(states.TryGetValue(id, out var state) && state.UserId == userId ? state : null);
    public async Task<AttemptState?> UpdateLockedAsync(string id, string userId, Func<AttemptState, AttemptState> update)
    {
        var state = await ReadAsync(id, userId);
        if (state is null) return null;
        var next = update(state);
        states[id] = next;
        return next;
    }
}

sealed class FakeUsers : IUserRepository
{
    public UserAccount User { get; set; } = null!;
    public Task<UserAccount?> FindByUsernameAsync(string username)
        => Task.FromResult<UserAccount?>(User.Username == username ? User : null);
    public Task<UserAccount?> FindByIdAsync(string id)
        => Task.FromResult<UserAccount?>(User.Id == id ? User : null);
    public Task CreateAsync(UserAccount user) { User = user; return Task.CompletedTask; }
    public Task<bool> ChangePasswordAsync(string id, string expectedStamp, string passwordHash, string newStamp)
    {
        if (User.Id != id || User.SecurityStamp != expectedStamp || User.Status != "ACTIVE") return Task.FromResult(false);
        User = User with { PasswordHash = passwordHash, SecurityStamp = newStamp };
        return Task.FromResult(true);
    }
}

sealed class TestEnvironment : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "PracticeChecks";
    public string ContentRootPath { get; set; } = ".";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = ".";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
