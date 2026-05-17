namespace Encyclopedia.Services.Mobile;

public interface IDeviceDetectionService
{
    bool IsMobile(string? userAgent);
    bool IsTablet(string? userAgent);
}
