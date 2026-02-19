using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LivekitAspire;

/// <summary>
/// LiveKit ingress configuration that can be serialized to YAML.
/// Configuration for streaming input services (RTMP, WHIP, URL pull).
/// </summary>
public class LivekitIngressConfiguration
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
    /// Redis configuration for ingress coordination.
    /// </summary>
    [YamlMember(Alias = "redis")]
    public RedisConfiguration? Redis { get; set; }

    /// <summary>
    /// RTMP port for streaming input.
    /// </summary>
    [YamlMember(Alias = "rtmp_port")]
    public int? RtmpPort { get; set; }

    /// <summary>
    /// WHIP port for WebRTC streaming input.
    /// </summary>
    [YamlMember(Alias = "whip_port")]
    public int? WhipPort { get; set; }

    /// <summary>
    /// HTTP relay port for internal communication.
    /// </summary>
    [YamlMember(Alias = "http_relay_port")]
    public int? HttpRelayPort { get; set; }

    /// <summary>
    /// Health check port.
    /// </summary>
    [YamlMember(Alias = "health_port")]
    public int? HealthPort { get; set; }

    /// <summary>
    /// Logging configuration.
    /// </summary>
    [YamlMember(Alias = "logging")]
    public LoggingConfiguration? Logging { get; set; }

    /// <summary>
    /// CPU costs for different input types.
    /// </summary>
    [YamlMember(Alias = "cpu_cost")]
    public IngressCpuCostConfiguration? CpuCost { get; set; }

    /// <summary>
    /// Enable insecure connections (skip TLS verification).
    /// </summary>
    [YamlMember(Alias = "insecure")]
    public bool? Insecure { get; set; }

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
    /// Deserializes a YAML string to a LivekitIngressConfiguration.
    /// </summary>
    public static LivekitIngressConfiguration FromYaml(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<LivekitIngressConfiguration>(yaml);
    }
}

/// <summary>
/// CPU cost configuration for ingress operations.
/// </summary>
public class IngressCpuCostConfiguration
{
    /// <summary>
    /// CPU cost for RTMP ingress.
    /// </summary>
    [YamlMember(Alias = "rtmp")]
    public double? Rtmp { get; set; }

    /// <summary>
    /// CPU cost for WHIP ingress.
    /// </summary>
    [YamlMember(Alias = "whip")]
    public double? Whip { get; set; }

    /// <summary>
    /// CPU cost for URL pull ingress.
    /// </summary>
    [YamlMember(Alias = "url")]
    public double? Url { get; set; }
}