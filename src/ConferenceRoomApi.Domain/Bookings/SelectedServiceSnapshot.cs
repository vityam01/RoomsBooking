namespace ConferenceRoomApi.Domain.Bookings;

/// <summary>
/// Input to <see cref="Booking.Create"/>: an additional service the client selected, already
/// resolved to its current catalog name/price by the caller. Booking.Create only snapshots
/// what it is given — looking the service up is the Application layer's job.
/// </summary>
public sealed record SelectedServiceSnapshot(Guid AdditionalServiceId, string Name, decimal Price);
