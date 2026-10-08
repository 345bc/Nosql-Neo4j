using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Neo4j.Driver;
using Nosql_Neo4j.Repositories;
namespace Nosql_Neo4j.Configuration;

public sealed class AccountCookieEvents(IUserRepository users) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        try
        {
            var user = id is null ? null : await users.FindByIdAsync(id);
            if (user is { Status: "ACTIVE" }
                && user.SecurityStamp == context.Principal?.FindFirstValue("security_stamp")
                && user.Role == context.Principal?.FindFirstValue(ClaimTypes.Role)) return;
        }
        catch (Neo4jException)
        {
            // Fail closed when account validity cannot be checked.
        }
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
