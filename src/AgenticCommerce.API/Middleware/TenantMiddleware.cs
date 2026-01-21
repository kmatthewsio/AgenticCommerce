using System.Security.Claims;

namespace AgenticCommerce.API.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract organization ID from authenticated user claims
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var orgIdClaim = context.User.FindFirst("organization_id")?.Value;
            if (Guid.TryParse(orgIdClaim, out var organizationId))
            {
                context.Items["OrganizationId"] = organizationId;
            }

            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                context.Items["UserId"] = userId;
            }
        }

        await _next(context);
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}

public static class HttpContextExtensions
{
    public static Guid? GetOrganizationId(this HttpContext context)
    {
        return context.Items.TryGetValue("OrganizationId", out var orgId) && orgId is Guid id
            ? id
            : null;
    }

    public static Guid? GetUserId(this HttpContext context)
    {
        return context.Items.TryGetValue("UserId", out var userId) && userId is Guid id
            ? id
            : null;
    }
}
