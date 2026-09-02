namespace ConferenceRoomApi.Domain.AdditionalServices;

/// <summary>
/// A catalog item that rooms can offer on top of the base rental (e.g. projector, Wi-Fi).
/// Named "AdditionalService" rather than "Service" to avoid clashing with the ubiquitous
/// dependency-injection meaning of "service" elsewhere in the codebase.
/// </summary>
public sealed class AdditionalService
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private AdditionalService()
    {
        // Required by EF Core.
    }

    public static AdditionalService Create(string name, decimal price)
    {
        var service = new AdditionalService();
        service.Id = Guid.NewGuid();
        service.SetName(name);
        service.SetPrice(price);
        service.IsActive = true;
        service.CreatedAt = DateTimeOffset.UtcNow;
        service.UpdatedAt = service.CreatedAt;
        return service;
    }

    public void Update(string name, decimal price)
    {
        SetName(name);
        SetPrice(price);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Additional service name must not be empty.", nameof(name));
        }

        Name = name.Trim();
    }

    private void SetPrice(decimal price)
    {
        if (price < 0)
        {
            throw new ArgumentException("Additional service price cannot be negative.", nameof(price));
        }

        Price = price;
    }
}
