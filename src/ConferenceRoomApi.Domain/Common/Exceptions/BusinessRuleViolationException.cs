namespace ConferenceRoomApi.Domain.Common.Exceptions;

/// <summary>
/// Thrown when a request is well-formed but violates a business invariant
/// (e.g. booking hours outside operating hours, invalid capacity). Maps to HTTP 400.
/// </summary>
public sealed class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string message) : base(message)
    {
    }
}
