using Neo4j.Driver;
using Microsoft.Extensions.Options;
using Nosql_Neo4j.Configuration;
using Nosql_Neo4j.Contracts;
using Nosql_Neo4j.Repositories;
using Nosql_Neo4j.Services;
using Nosql_Neo4j.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.RateLimiting;



var builder = WebApplication.CreateBuilder(args);
builder.AddLocalNeo4jEnv();



// Add services to the container.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "NosqlNeo4j.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
        options.EventsType = typeof(AccountCookieEvents);
    });
builder.Services.AddScoped<AccountCookieEvents>();
builder.Services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<IPracticeRepository, PracticeRepository>();
builder.Services.AddScoped<PracticeReportRepository>();
builder.Services.AddScoped<IPracticeReportService, PracticeReportService>();
builder.Services.AddScoped<IPracticeService, PracticeService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});


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
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IQuestionDiagnosticService, QuestionDiagnosticService>();

builder.Services.AddScoped<IKnowledgeSnapshotRepository, Neo4jShapeRepository>();
builder.Services.AddScoped<IShapeService, ShapeService>();
var app = builder.Build();

await app.Services
    .GetRequiredService<IDriver>()
    .VerifyConnectivityAsync();

var createUserIndex = Array.IndexOf(args, "--create-user");
if (args.Contains("--setup-data") || args.Contains("--verify-data") || args.Contains("--migrate-attempts"))
{
    if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Công cụ dữ liệu local chỉ chạy trong Development.");
    var driver = app.Services.GetRequiredService<IDriver>();
    if (args.Contains("--setup-data")) await LocalDataCommand.SetupAsync(driver, app.Environment.ContentRootPath);
    else if (args.Contains("--migrate-attempts")) await LocalDataCommand.MigrateAttemptsAsync(driver);
    await LocalDataCommand.VerifyAsync(driver);
    await app.DisposeAsync();
    return;
}
if (createUserIndex >= 0)
{
    if (!app.Environment.IsDevelopment() || createUserIndex + 1 >= args.Length)
        throw new InvalidOperationException("Dùng --create-user <username> trong Development.");
    await LocalAccountCommand.RunAsync(app.Services, args[createUserIndex + 1]);
    await app.DisposeAsync();
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

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