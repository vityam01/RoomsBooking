namespace ConferenceRoomApi.Application.Common.Dtos;

/// <summary>A page of results plus enough metadata for a client to fetch the next one.</summary>
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
