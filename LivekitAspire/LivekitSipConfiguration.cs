using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LivekitAspire;

/// <summary>
/// LiveKit SIP configuration that can be serialized to YAML.
/// Configuration for telephony integration with SIP/PSTN networks.
/// </summary>
public class LivekitSipConfiguration
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
    /// Redis configuration for SIP coordination.
    /// </summary>
    [YamlMember(Alias = "redis")]
    public RedisConfiguration? Redis { get; set; }

    /// <summary>
    /// SIP signaling port.
    /// </summary>
    [YamlMember(Alias = "sip_port")]
    public int? SipPort { get; set; }

    /// <summary>
    /// RTP port range for media transport (e.g., "10000-20000").
    /// </summary>
    [YamlMember(Alias = "rtp_port")]
    public string? RtpPort { get; set; }

    /// <summary>
    /// Logging configuration.
    /// </summary>
    [YamlMember(Alias = "logging")]
    public LoggingConfiguration? Logging { get; set; }

    /// <summary>
    /// Hide phone numbers in logs for privacy.
    /// </summary>
    [YamlMember(Alias = "hide_phone_number")]
    public bool? HidePhoneNumber { get; set; }

    /// <summary>
    /// Enable insecure connections (skip TLS verification).
    /// </summary>
    [YamlMember(Alias = "insecure")]
    public bool? Insecure { get; set; }

    /// <summary>
    /// Health check configuration.
    /// </summary>
    [YamlMember(Alias = "health")]
    public HealthConfiguration? Health { get; set; }

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
    /// Deserializes a YAML string to a LivekitSipConfiguration.
    /// </summary>
    public static LivekitSipConfiguration FromYaml(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<LivekitSipConfiguration>(yaml);
    }
}

/// <summary>
/// Health check configuration.
/// </summary>
public class HealthConfiguration
{
    /// <summary>
    /// Port for health checks.
    /// </summary>
    [YamlMember(Alias = "port")]
    public int? Port { get; set; }
}