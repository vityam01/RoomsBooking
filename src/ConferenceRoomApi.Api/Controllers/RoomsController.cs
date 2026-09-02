using ConferenceRoomApi.Application.Rooms;
using ConferenceRoomApi.Application.Rooms.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomApi.Api.Controllers;

/// <summary>Manage conference rooms and search for availability.</summary>
[ApiController]
[Route("api/rooms")]
[Produces("application/json")]
public sealed class RoomsController : ControllerBase
{
    private readonly RoomsService _roomsService;

    public RoomsController(RoomsService roomsService)
    {
        _roomsService = roomsService;
    }

    /// <summary>List rooms.</summary>
    /// <param name="includeInactive">Include rooms that have been removed. Defaults to false.</param>
    /// <param name="cancellationToken"></param>
    [HttpGet]
    [ProducesResponseType(typeof(List<RoomDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RoomDto>>> List([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
        => Ok(await _roomsService.ListAsync(includeInactive, cancellationToken));

    /// <summary>Get a single room by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _roomsService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Search for rooms that are free for the requested date/time window and meet the
    /// minimum capacity. Each result includes an itemized price estimate for that exact slot.
    /// </summary>
    [HttpGet("available")]
    [ProducesResponseType(typeof(List<AvailableRoomDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<AvailableRoomDto>>> SearchAvailable(
        [FromQuery] DateOnly date, [FromQuery] TimeOnly startTime, [FromQuery] TimeOnly endTime, [FromQuery] int capacity,
        CancellationToken cancellationToken)
    {
        var request = new SearchAvailableRoomsRequest(date, startTime, endTime, capacity);
        return Ok(await _roomsService.SearchAvailableAsync(request, cancellationToken));
    }

    /// <summary>Register a new conference room.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RoomDto>> Create([FromBody] CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var room = await _roomsService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
    }

    /// <summary>Update a room's name, capacity, base price, and offered additional services.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomDto>> Update(Guid id, [FromBody] UpdateRoomRequest request, CancellationToken cancellationToken)
        => Ok(await _roomsService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Remove a room. Soft delete: past bookings keep referencing it for reporting.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _roomsService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
