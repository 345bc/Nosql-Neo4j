namespace Nosql_Neo4j.Configuration;

public static class Neo4jDatabaseConfiguration
{
    public static string GetNeo4jDatabaseName(this IConfiguration configuration)
    {
        var database = configuration["Neo4j:Database"] ?? "nosql-neo4j";
        if (string.IsNullOrWhiteSpace(database))
            throw new InvalidOperationException("Neo4j:Database không được để trống.");
        return database;
    }
}
