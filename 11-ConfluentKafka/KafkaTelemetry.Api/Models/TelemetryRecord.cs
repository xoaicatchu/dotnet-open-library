namespace KafkaTelemetry.Api.Models;

public record TelemetryRecord(string DeviceId, double Temperature, double Humidity, DateTime Timestamp);
