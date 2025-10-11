namespace Ombor.Domain.Common;

public sealed class Address
{
    public required decimal Latitude { get; set; }
    public required decimal Longtitude { get; set; }

    public Address()
    {
    }

    public Address(decimal latitude, decimal longitude)
    {
        Latitude = latitude;
        Longtitude = longitude;
    }
}
