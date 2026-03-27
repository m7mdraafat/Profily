using Profily.Core.Entities;

namespace Profily.Api.Extensions;

public static class HttpContextExtensions
{
    public static Guid GetUserId(this HttpContext context)
        => context.Items.TryGetValue("UserId", out var id)
            ? (Guid)id!
            : throw new InvalidOperationException("UserId not found. Ensure the request passed through SessionAuthMiddleware.");

    public static User GetUser(this HttpContext context)
        => context.Items.TryGetValue("User", out var user)
            ? (User)user!
            : throw new InvalidOperationException("User not found. Ensure the request passed through SessionAuthMiddleware.");
}