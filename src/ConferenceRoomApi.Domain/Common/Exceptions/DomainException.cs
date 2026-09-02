namespace ConferenceRoomApi.Domain.Common.Exceptions;

/// <summary>
/// Base type for every exception that represents a violation of a business rule.
/// Kept separate from framework/validation exceptions so the API layer can map
/// domain failures to the correct HTTP status without inspecting exception text.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
