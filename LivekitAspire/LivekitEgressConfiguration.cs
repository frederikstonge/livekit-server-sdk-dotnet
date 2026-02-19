using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LivekitAspire;

/// <summary>
/// LiveKit egress configuration that can be serialized to YAML.
/// Configuration for recording and streaming output services.
/// </summary>
public class LivekitEgressConfiguration
{
    /// <summary>
    /// API key for authentication with LiveKit server.
    /// </summary>
    [YamlMember(Alias = "api_key")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// API secret for authentication with LiveKit server.
    /// </summary>
    [YamlMember(Alias = "api_secret")]
    public string? ApiSecret { get; set; }

    /// <summary>
    /// WebSocket URL of the LiveKit server.
    /// </summary>
    [YamlMember(Alias = "ws_url")]
    public string? WsUrl { get; set; }

    /// <summary>
    /// Enable insecure connections (skip TLS verification).
    /// </summary>
    [YamlMember(Alias = "insecure")]
    public bool? Insecure { get; set; }

    /// <summary>
    /// Health check port.
    /// </summary>
    [YamlMember(Alias = "health_port")]
    public int? HealthPort { get; set; }

    /// <summary>
    /// Redis configuration for egress coordination.
    /// </summary>
    [YamlMember(Alias = "redis")]
    public RedisConfiguration? Redis { get; set; }

    /// <summary>
    /// Logging configuration.
    /// </summary>
    [YamlMember(Alias = "logging")]
    public LoggingConfiguration? Logging { get; set; }

    /// <summary>
    /// Template base path for recordings.
    /// </summary>
    [YamlMember(Alias = "template_base")]
    public string? TemplateBase { get; set; }

    /// <summary>
    /// CPU costs for different output types.
    /// </summary>
    [YamlMember(Alias = "cpu_cost")]
    public CpuCostConfiguration? CpuCost { get; set; }

    /// <summary>
    /// Maximum concurrent recordings.
    /// </summary>
    [YamlMember(Alias = "session_limits")]
    public SessionLimitsConfiguration? SessionLimits { get; set; }

    /// <summary>
    /// Serializes this configuration to YAML.
    /// </summary>
    public string ToYaml()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        return serializer.Serialize(this);
    }

    /// <summary>
    /// Deserializes a YAML string to a LivekitEgressConfiguration.
    /// </summary>
    public static LivekitEgressConfiguration FromYaml(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<LivekitEgressConfiguration>(yaml);
    }
}

/// <summary>
/// CPU cost configuration for egress operations.
/// </summary>
public class CpuCostConfiguration
{
    /// <summary>
    /// CPU cost for room composite recording.
    /// </summary>
    [YamlMember(Alias = "room_composite")]
    public double? RoomComposite { get; set; }

    /// <summary>
    /// CPU cost for web recording.
    /// </summary>
    [YamlMember(Alias = "web")]
    public double? Web { get; set; }

    /// <summary>
    /// CPU cost for participant composite recording.
    /// </summary>
    [YamlMember(Alias = "participant_composite")]
    public double? ParticipantComposite { get; set; }

    /// <summary>
    /// CPU cost for track composite recording.
    /// </summary>
    [YamlMember(Alias = "track_composite")]
    public double? TrackComposite { get; set; }

    /// <summary>
    /// CPU cost for track recording.
    /// </summary>
    [YamlMember(Alias = "track")]
    public double? Track { get; set; }
}

/// <summary>
/// Session limits configuration for egress.
/// </summary>
public class SessionLimitsConfiguration
{
    /// <summary>
    /// Maximum file output sessions.
    /// </summary>
    [YamlMember(Alias = "file_output_max_sessions")]
    public int? FileOutputMaxSessions { get; set; }

    /// <summary>
    /// Maximum stream output sessions.
    /// </summary>
    [YamlMember(Alias = "stream_output_max_sessions")]
    public int? StreamOutputMaxSessions { get; set; }

    /// <summary>
    /// Maximum segment output sessions.
    /// </summary>
    [YamlMember(Alias = "segment_output_max_sessions")]
    public int? SegmentOutputMaxSessions { get; set; }

    /// <summary>
    /// Maximum image output sessions.
    /// </summary>
    [YamlMember(Alias = "image_output_max_sessions")]
    public int? ImageOutputMaxSessions { get; set; }
}