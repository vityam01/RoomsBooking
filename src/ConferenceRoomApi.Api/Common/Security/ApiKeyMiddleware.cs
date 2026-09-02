using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ConferenceRoomApi.Api.Common.Security;

/// <summary>
/// Requires a valid X-Api-Key header on state-changing requests (POST/PUT/PATCH/DELETE)
/// when at least one key is configured. Read-only requests (GET, search, reports) and the
/// Swagger/health endpoints are never gated, so the API stays browsable without a key.
/// </summary>
public sealed class ApiKeyMiddleware
{
    private const string HeaderName = "X-Api-Key";
    private static readonly string[] ExemptPathPrefixes = { "/swagger", "/health" };
    private static readonly HashSet<string> ProtectedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete
    };

    private readonly RequestDelegate _next;

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<ApiKeyOptions> options)
    {
        var configuredKeys = options.Value.ApiKeys;
        var isProtectedMethod = ProtectedMethods.Contains(context.Request.Method);
        var isExemptPath = ExemptPathPrefixes.Any(p => context.Request.Path.StartsWithSegments(p));

        if (configuredKeys.Count == 0 || !isProtectedMethod || isExemptPath)
        {
            await _next(context);
            return;
        }

        var providedKey = context.Request.Headers[HeaderName].FirstOrDefault();
        if (providedKey is null || !configuredKeys.Contains(providedKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = $"A valid {HeaderName} header is required for this operation.",
                Instance = context.Request.Path
            });
            return;
        }

        await _next(context);
    }
}
