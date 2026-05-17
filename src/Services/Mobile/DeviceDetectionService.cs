using UAParser;

namespace Encyclopedia.Services.Mobile;

public sealed class DeviceDetectionService : IDeviceDetectionService
{
    private readonly Parser _parser = Parser.GetDefault();

    public bool IsMobile(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return false;
        var info = _parser.Parse(userAgent);
        var family = info.Device.Family;
        if (string.Equals(family, "Other", StringComparison.OrdinalIgnoreCase)) return false;
        return !string.Equals(family, "iPad", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsTablet(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return false;
        var info = _parser.Parse(userAgent);
        return string.Equals(info.Device.Family, "iPad", StringComparison.OrdinalIgnoreCase);
    }
}
