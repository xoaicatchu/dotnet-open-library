namespace SmartHomeHub.Api.Models;

public class DeviceState
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

public class DeviceCommand
{
    public string DeviceType { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
}
