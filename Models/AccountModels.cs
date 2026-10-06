using System.ComponentModel.DataAnnotations;
namespace Nosql_Neo4j.Models;

public sealed record UserAccount(string Id, string Username, string DisplayName,
    string PasswordHash, string Role, string Status, string SecurityStamp);

public sealed class LoginInput
{
    [Required(ErrorMessage = "Nhập tên đăng nhập."), StringLength(50)]
    public string Username { get; set; } = "";
    [Required(ErrorMessage = "Nhập mật khẩu."), StringLength(256), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public sealed class ChangePasswordInput
{
    [Required(ErrorMessage = "Nhập mật khẩu hiện tại."), StringLength(256), DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = "";
    [Required(ErrorMessage = "Nhập mật khẩu mới."), StringLength(256, MinimumLength = 12,
        ErrorMessage = "Mật khẩu mới cần 12–256 ký tự."), DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";
    [Compare(nameof(NewPassword), ErrorMessage = "Hai mật khẩu mới chưa khớp."), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}
