namespace ConferenceRoomApi.Application.Common.Interfaces;

/// <summary>
/// Commits the changes staged across one or more repositories in a single transaction.
/// Repositories only stage changes (Add/Update/Remove on the in-memory tracked graph);
/// nothing hits the database until a use case explicitly calls SaveChangesAsync, which
/// keeps the transaction boundary at the use case rather than scattered through the code.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
