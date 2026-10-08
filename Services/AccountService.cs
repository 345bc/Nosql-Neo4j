using Microsoft.AspNetCore.Identity;
using Nosql_Neo4j.Models;
using Nosql_Neo4j.Repositories;
namespace Nosql_Neo4j.Services;

public sealed class AccountService(IUserRepository repository, IPasswordHasher<UserAccount> hasher)
{
    private static readonly UserAccount Dummy = new("", "", "", "", "USER", "ACTIVE", "");
    // Equal-cost verification when username is absent.
    private static readonly string DummyHash = new PasswordHasher<UserAccount>()
        .HashPassword(Dummy, Guid.NewGuid().ToString());

    public async Task<UserAccount?> AuthenticateAsync(string username, string password)
    {
        var user = await repository.FindByUsernameAsync(username);
        var verification = hasher.VerifyHashedPassword(user ?? Dummy, user?.PasswordHash ?? DummyHash, password);
        return user is { Status: "ACTIVE" } && verification != PasswordVerificationResult.Failed ? user : null;
    }

    public async Task CreateLocalUserAsync(string username, string password)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-zA-Z0-9._-]{3,50}$"))
            throw new ArgumentException("Tên đăng nhập gồm 3–50 ký tự chữ, số, dấu chấm, gạch ngang hoặc gạch dưới.");
        if (password.Length < 12 || password.Length > 256)
            throw new ArgumentException("Mật khẩu cần 12–256 ký tự.");
        if (await repository.FindByUsernameAsync(username) is not null)
            throw new ArgumentException("Tên đăng nhập đã tồn tại.");
        var user = new UserAccount(Guid.NewGuid().ToString("N"), username, username, "",
            "USER", "ACTIVE", Guid.NewGuid().ToString("N"));
        await repository.CreateAsync(user with { PasswordHash = hasher.HashPassword(user, password) });
    }

    public async Task<bool> ChangePasswordAsync(string id, string currentPassword, string newPassword)
    {
        if (newPassword.Length is < 12 or > 256 || newPassword == currentPassword) return false;
        var user = await repository.FindByIdAsync(id);
        if (user is not { Status: "ACTIVE" } ||
            hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
            return false;
        return await repository.ChangePasswordAsync(id, user.SecurityStamp,
            hasher.HashPassword(user, newPassword), Guid.NewGuid().ToString("N"));
    }
}
