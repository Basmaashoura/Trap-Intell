namespace Trap_Intel.Domain.Attacks;

/// <summary>
/// Interface for attack event data received from external sources.
/// This allows the domain to define what data it needs without knowing
/// about specific transport mechanisms (gRPC, HTTP, etc.).
/// 
/// Implementation lives in Infrastructure/Application layer as DTOs.
/// </summary>
public interface IAttackEventData
{
    string ExternalEventId { get; }
    DateTime Timestamp { get; }
    string EventType { get; }
    string Severity { get; }
    string SourceIP { get; }
    int SourcePort { get; }
    int TargetPort { get; }
    string? SensorId { get; }
    string? Protocol { get; }
    long SessionId { get; }
    bool WasEdgeFiltered { get; }
    string? FilterReason { get; }
    
    // Optional captured data
    string? Username { get; }
    string? Password { get; }
    string? Command { get; }
    byte[]? Payload { get; }
    string? UserAgent { get; }
    IReadOnlyDictionary<string, string>? Headers { get; }
    
    // Geolocation
    IGeoLocationData? Geolocation { get; }
    
    // Raw JSON payload
    string? RawPayloadJson { get; }
}

/// <summary>
/// Interface for geolocation data.
/// Implementation lives in Infrastructure/Application layer.
/// </summary>
public interface IGeoLocationData
{
    string? Country { get; }
    string? CountryCode { get; }
    string? City { get; }
    decimal? Latitude { get; }
    decimal? Longitude { get; }
    string? Region { get; }
    string? ISP { get; }
    string? ASN { get; }
}
