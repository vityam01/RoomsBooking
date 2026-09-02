using ConferenceRoomApi.Application.AdditionalServices;
using ConferenceRoomApi.Application.AdditionalServices.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomApi.Api.Controllers;

/// <summary>Manage the catalog of additional services rooms can offer (projector, Wi-Fi, sound, ...).</summary>
[ApiController]
[Route("api/additional-services")]
[Produces("application/json")]
public sealed class AdditionalServicesController : ControllerBase
{
    private readonly AdditionalServicesService _service;

    public AdditionalServicesController(AdditionalServicesService service)
    {
        _service = service;
    }

    /// <summary>List active additional services.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AdditionalServiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AdditionalServiceDto>>> List(CancellationToken cancellationToken)
        => Ok(await _service.ListAsync(cancellationToken));

    /// <summary>Get a single additional service by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdditionalServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdditionalServiceDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetByIdAsync(id, cancellationToken));

    /// <summary>Add a new additional service to the catalog.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AdditionalServiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdditionalServiceDto>> Create([FromBody] CreateAdditionalServiceRequest request, CancellationToken cancellationToken)
    {
        var service = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = service.Id }, service);
    }

    /// <summary>Update an additional service's name and price.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AdditionalServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdditionalServiceDto>> Update(Guid id, [FromBody] UpdateAdditionalServiceRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Remove an additional service from the catalog (soft delete).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
