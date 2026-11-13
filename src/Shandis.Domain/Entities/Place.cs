using Shandis.Domain.Common;

namespace Shandis.Domain.Entities;

public class Place : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    public ICollection<Memory> Memories { get; private set; } = new List<Memory>();

    private Place() { }

    public Place(
        string name,
        string? address = null,
        double? latitude = null,
        double? longitude = null)
    {
        ValidateCoordinates(latitude, longitude);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Place name is required.", nameof(name));

        Name = name.Trim();
        Address = address?.Trim();
        Latitude = latitude;
        Longitude = longitude;
    }

    public void Update(
        string name,
        string? address,
        double? latitude,
        double? longitude)
    {
        ValidateCoordinates(latitude, longitude);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Place name is required.", nameof(name));

        Name = name.Trim();
        Address = address?.Trim();
        Latitude = latitude;
        Longitude = longitude;

        MarkAsUpdated();
    }

    private static void ValidateCoordinates(double? latitude, double? longitude)
    {
        if (latitude.HasValue && (latitude < -90 || latitude > 90))
            throw new ArgumentOutOfRangeException(nameof(latitude));

        if (longitude.HasValue && (longitude < -180 || longitude > 180))
            throw new ArgumentOutOfRangeException(nameof(longitude));

        if (latitude.HasValue != longitude.HasValue)
            throw new ArgumentException("Latitude and longitude must either both be provided or both be null.");
    }
}
