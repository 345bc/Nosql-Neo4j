namespace Nosql_Neo4j.Configuration;

public static class LocalEnvConfiguration
{
    // Minimal local format: KEY=value, optional matching quotes, full-line comments.
    // No variable expansion, inline comments or multiline values.
    public static void AddLocalNeo4jEnv(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment()) return;

        var path = Path.Combine(builder.Environment.ContentRootPath, ".env");
        if (!File.Exists(path)) return;

        var values = new Dictionary<string, string?>();
        var allowedKeys = new HashSet<string>
        {
            "Neo4j__Uri", "Neo4j__Username", "Neo4j__Password", "Neo4j__Database"
        };

        var lineNumber = 0;
        foreach (var rawLine in File.ReadLines(path))
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            var separator = line.IndexOf('=');
            if (separator < 1)
                throw new InvalidOperationException($".env: dòng {lineNumber} phải có dạng KEY=value.");

            var key = line[..separator].Trim();
            if (!allowedKeys.Contains(key))
                throw new InvalidOperationException($".env: khóa không được hỗ trợ ở dòng {lineNumber}.");

            var value = line[(separator + 1)..].Trim();
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') ||
                 (value[0] == '\'' && value[^1] == '\'')))
                value = value[1..^1];

            if (!values.TryAdd(key.Replace("__", ":"), value))
                throw new InvalidOperationException($".env: khóa trùng ở dòng {lineNumber}.");
        }

        // Local file overrides appsettings/user-secrets; real environment overrides the file.
        builder.Configuration.AddInMemoryCollection(values);
        builder.Configuration.AddEnvironmentVariables();
    }
}
