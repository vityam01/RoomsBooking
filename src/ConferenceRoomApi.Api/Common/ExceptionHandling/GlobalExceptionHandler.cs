using ConferenceRoomApi.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ConferenceRoomApi.Api.Common.ExceptionHandling;

/// <summary>
/// Single place where every unhandled exception becomes an RFC 7807 ProblemDetails
/// response. Domain exceptions map to their intended status code; a Postgres exclusion
/// -constraint violation (SQLSTATE 23P01) is the database's own last line of defense
/// against a double-booking race and is translated to the same 409 a same-request overlap
/// check would produce. Everything else is logged in full and returned as an opaque 500 —
/// stack traces and connection strings never reach the client.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const string ExclusionViolationSqlState = "23P01";

    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = Classify(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "{Title} on {Method} {Path}", title, httpContext.Request.Method, httpContext.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private const string ConcurrentBookingConflictMessage =
        "This room was just booked for an overlapping time slot by another request. Please choose a different time.";

    private static (int StatusCode, string Title, string Detail) Classify(Exception exception) => exception switch
    {
        EntityNotFoundException => (StatusCodes.Status404NotFound, "Not Found", exception.Message),
        RoomUnavailableException => (StatusCodes.Status409Conflict, "Room Unavailable", exception.Message),
        BusinessRuleViolationException => (StatusCodes.Status400BadRequest, "Business Rule Violation", exception.Message),
        // EF Core wraps the driver error in DbUpdateException; the PostgresException itself
        // is the InnerException. Matched by SQLSTATE, not message text, so it survives locale changes.
        DbUpdateException { InnerException: PostgresException { SqlState: ExclusionViolationSqlState } }
            => (StatusCodes.Status409Conflict, "Room Unavailable", ConcurrentBookingConflictMessage),
        PostgresException { SqlState: ExclusionViolationSqlState }
            => (StatusCodes.Status409Conflict, "Room Unavailable", ConcurrentBookingConflictMessage),
        _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.")
    };
}
