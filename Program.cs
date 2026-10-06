using Neo4j.Driver;
using Nosql_Neo4j.Configuration;
using Nosql_Neo4j.Repositories;
using Nosql_Neo4j.Services;



var builder = WebApplication.CreateBuilder(args);
builder.AddLocalNeo4jEnv();



// Add services to the container.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));

builder.Services.AddSingleton<IDriver>(_ =>
{
    var config = builder.Configuration;

    if (string.IsNullOrWhiteSpace(config["Neo4j:Password"]) ||
        config["Neo4j:Password"] == "CHANGE_ME")
        throw new InvalidOperationException("Điền mật khẩu Neo4j thật vào .env (Neo4j__Password) trước khi chạy.");

    return GraphDatabase.Driver(
        config["Neo4j:Uri"]
            ?? throw new InvalidOperationException("Thiếu Neo4j:Uri"),
        AuthTokens.Basic(
            config["Neo4j:Username"]
                ?? throw new InvalidOperationException("Thiếu Neo4j:Username"),
            config["Neo4j:Password"]
                ?? throw new InvalidOperationException("Thiếu Neo4j:Password")));
});

builder.Services.AddScoped<IShapeRepository, ShapeRepository>();
builder.Services.AddScoped<IShapeDiagnosticService, ShapeDiagnosticService>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IQuestionDiagnosticService, QuestionDiagnosticService>();

var app = builder.Build();

await app.Services
    .GetRequiredService<IDriver>()
    .VerifyConnectivityAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
