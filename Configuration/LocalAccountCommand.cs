using Nosql_Neo4j.Services;
namespace Nosql_Neo4j.Configuration;

public static class LocalAccountCommand
{
    public static async Task RunAsync(IServiceProvider services, string username)
    {
        Console.Write("Mật khẩu tài khoản mới (không hiển thị): ");
        var password = ReadPassword();
        Console.Write("Nhập lại mật khẩu: ");
        var confirm = ReadPassword();
        if (password != confirm) throw new ArgumentException("Mật khẩu nhập lại không khớp.");
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AccountService>()
            .CreateLocalUserAsync(username, password);
        Console.WriteLine("Đã tạo tài khoản USER. Đăng nhập tại /Account/Login.");
    }

    private static string ReadPassword()
    {
        if (Console.IsInputRedirected)
            throw new InvalidOperationException("Chạy lệnh trong terminal tương tác để nhập mật khẩu.");
        var characters = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return new string(characters.ToArray()); }
            if (key.Key == ConsoleKey.Backspace) { if (characters.Count > 0) characters.RemoveAt(characters.Count - 1); }
            else if (!char.IsControl(key.KeyChar) && characters.Count < 256) characters.Add(key.KeyChar);
        }
    }
}
