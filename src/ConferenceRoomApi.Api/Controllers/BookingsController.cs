using ConferenceRoomApi.Application.Bookings;
using ConferenceRoomApi.Application.Bookings.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomApi.Api.Controllers;

/// <summary>Book rooms and manage existing bookings.</summary>
[ApiController]
[Route("api/bookings")]
[Produces("application/json")]
public sealed class BookingsController : ControllerBase
{
    private readonly BookingsService _bookingsService;

    public BookingsController(BookingsService bookingsService)
    {
        _bookingsService = bookingsService;
    }

    /// <summary>
    /// Book a room for a date/time window with an optional set of additional services.
    /// The response includes the full itemized cost breakdown by rate zone.
    /// </summary>
    /// <response code="201">Booking confirmed.</response>
    /// <response code="400">The request is invalid (bad hours, unknown service, etc.).</response>
    /// <response code="404">The room does not exist.</response>
    /// <response code="409">The room is already booked for an overlapping time.</response>
    [HttpPost]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingDto>> Create([FromBody] CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = await _bookingsService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, booking);
    }

    /// <summary>Get a single booking by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _bookingsService.GetByIdAsync(id, cancellationToken));

    /// <summary>List bookings, optionally filtered by room and date range.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<BookingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BookingDto>>> List(
        [FromQuery] Guid? roomId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] bool includeCancelled = false, CancellationToken cancellationToken = default)
    {
        var filter = new BookingListFilter(roomId, from, to, includeCancelled);
        return Ok(await _bookingsService.ListAsync(filter, cancellationToken));
    }

    /// <summary>Cancel a booking.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _bookingsService.CancelAsync(id, cancellationToken);
        return NoContent();
    }
}
