using Nosql_Neo4j.Models;
namespace Nosql_Neo4j.Repositories;

public interface IUserRepository
{
    Task<UserAccount?> FindByUsernameAsync(string username);
    Task<UserAccount?> FindByIdAsync(string id);
    Task CreateAsync(UserAccount user);
    Task<bool> ChangePasswordAsync(string id, string expectedStamp, string passwordHash, string newStamp);
}
