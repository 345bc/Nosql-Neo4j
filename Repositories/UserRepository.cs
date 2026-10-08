using Neo4j.Driver;
using Nosql_Neo4j.Models;
namespace Nosql_Neo4j.Repositories;

public sealed class UserRepository(IDriver driver) : IUserRepository
{
    public async Task<bool> ChangePasswordAsync(string id, string expectedStamp, string passwordHash, string newStamp)
    {
        await using var session = driver.AsyncSession(c => c.WithDatabase("nosql-neo4j"));
        return await session.ExecuteWriteAsync(async tx =>
        {
            // Acquire the node lock before checking the stamp, so simultaneous changes cannot both succeed.
            var locked = await tx.RunAsync("MATCH (u:User {id: $id}) SET u.lockRevision = coalesce(u.lockRevision, 0) + 1 RETURN u.id", new { id });
            await locked.ConsumeAsync();
            var result = await tx.RunAsync("""
                MATCH (u:User {id: $id, status: 'ACTIVE', securityStamp: $expectedStamp})
                SET u.passwordHash = $passwordHash, u.securityStamp = $newStamp
                CREATE (e:AuditEvent {id: randomUUID(), action: 'PASSWORD_CHANGED', at: datetime()})
                CREATE (u)-[:PERFORMED]->(e)
                RETURN u.id AS id
                """, new { id, expectedStamp, passwordHash, newStamp });
            return await result.FetchAsync();
        });
    }
    public Task<UserAccount?> FindByUsernameAsync(string username)
        => FindAsync("u.normalizedUsername = $value", username.Trim().ToUpperInvariant());
    public Task<UserAccount?> FindByIdAsync(string id) => FindAsync("u.id = $value", id);

    private async Task<UserAccount?> FindAsync(string predicate, string value)
    {
        await using var session = driver.AsyncSession(c => c.WithDatabase("nosql-neo4j"));
        return await session.ExecuteReadAsync<UserAccount?>(async tx =>
        {
            // predicate chỉ là một trong hai chuỗi cố định ở trên; value luôn tham số hóa.
            var cursor = await tx.RunAsync(
                "MATCH (u:User) WHERE " + predicate +
                " RETURN u.id AS id, u.username AS username, u.displayName AS displayName," +
                " u.passwordHash AS passwordHash, u.role AS role, u.status AS status, u.securityStamp AS stamp",
                new { value });
            if (!await cursor.FetchAsync()) return null;
            var r = cursor.Current;
            return new UserAccount(r["id"].As<string>(), r["username"].As<string>(),
                r["displayName"].As<string>(), r["passwordHash"].As<string>(),
                r["role"].As<string>(), r["status"].As<string>(), r["stamp"].As<string>());
        });
    }

    public async Task CreateAsync(UserAccount user)
    {
        await using var session = driver.AsyncSession(c => c.WithDatabase("nosql-neo4j"));
        // Lệnh CLI tạo tài khoản local cũng thiết lập constraints trước khi tạo.
        await (await session.RunAsync("CREATE CONSTRAINT user_id_unique IF NOT EXISTS FOR (u:User) REQUIRE u.id IS UNIQUE")).ConsumeAsync();
        await (await session.RunAsync("CREATE CONSTRAINT user_username_unique IF NOT EXISTS FOR (u:User) REQUIRE u.normalizedUsername IS UNIQUE")).ConsumeAsync();
        await session.ExecuteWriteAsync(async tx =>
        {
            await (await tx.RunAsync("""
                CREATE (u:User {id: $id, username: $username, normalizedUsername: $normalized,
                    displayName: $displayName, passwordHash: $passwordHash, role: $role,
                    status: $status, securityStamp: $stamp})
                """, new { id = user.Id, username = user.Username,
                normalized = user.Username.ToUpperInvariant(), displayName = user.DisplayName,
                passwordHash = user.PasswordHash, role = user.Role, status = user.Status,
                stamp = user.SecurityStamp })).ConsumeAsync();
        });
    }
}
