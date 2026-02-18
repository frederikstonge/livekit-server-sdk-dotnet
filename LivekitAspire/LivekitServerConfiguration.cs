using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LivekitAspire;

/// <summary>
/// LiveKit server configuration that can be serialized to YAML.
/// Based on https://github.com/livekit/livekit/blob/master/config-sample.yaml
/// </summary>
public class LivekitServerConfiguration
{
    /// <summary>
    /// Main TCP port for RoomService and RTC endpoint.
    /// For production setups, this port should be placed behind a load balancer with TLS.
    /// </summary>
    [YamlMember(Alias = "port")]
    public int? Port { get; set; }

    /// <summary>
    /// Redis configuration for distributed operation.
    /// When set, LiveKit will automatically operate in a fully distributed fashion.
    /// </summary>
    [YamlMember(Alias = "redis")]
    public RedisConfiguration? Redis { get; set; }

    /// <summary>
    /// WebRTC configuration.
    /// </summary>
    [YamlMember(Alias = "rtc")]
    public RtcConfiguration? Rtc { get; set; }

    /// <summary>
    /// API key / secret pairs for JWT authentication.
    /// </summary>
    [YamlMember(Alias = "keys")]
    public Dictionary<string, string>? Keys { get; set; }

    /// <summary>
    /// Logging configuration.
    /// </summary>
    [YamlMember(Alias = "logging")]
    public LoggingConfiguration? Logging { get; set; }

    /// <summary>
    /// Default room configuration.
    /// </summary>
    [YamlMember(Alias = "room")]
    public RoomConfiguration? Room { get; set; }

    /// <summary>
    /// Webhook configuration.
    /// </summary>
    [YamlMember(Alias = "webhook")]
    public WebhookConfiguration? Webhook { get; set; }

    /// <summary>
    /// TURN server configuration.
    /// </summary>
    [YamlMember(Alias = "turn")]
    public TurnConfiguration? Turn { get; set; }

    /// <summary>
    /// Ingress server configuration.
    /// </summary>
    [YamlMember(Alias = "ingress")]
    public IngressConfiguration? Ingress { get; set; }

    /// <summary>
    /// Region of the current node. Required if using regionaware node selector.
    /// </summary>
    [YamlMember(Alias = "region")]
    public string? Region { get; set; }

    /// <summary>
    /// Node selector configuration.
    /// </summary>
    [YamlMember(Alias = "node_selector")]
    public NodeSelectorConfiguration? NodeSelector { get; set; }

    /// <summary>
    /// Node limits configuration.
    /// </summary>
    [YamlMember(Alias = "limit")]
    public LimitConfiguration? Limit { get; set; }

    /// <summary>
    /// Audio level sensitivity configuration.
    /// </summary>
    [YamlMember(Alias = "audio")]
    public AudioConfiguration? Audio { get; set; }

    /// <summary>
    /// Prometheus metrics port.
    /// </summary>
    [YamlMember(Alias = "prometheus_port")]
    public int? PrometheusPort { get; set; }

    /// <summary>
    /// Signal relay configuration.
    /// </summary>
    [YamlMember(Alias = "signal_relay")]
    public SignalRelayConfiguration? SignalRelay { get; set; }

    /// <summary>
    /// PSRPC configuration.
    /// </summary>
    [YamlMember(Alias = "psrpc")]
    public PsrpcConfiguration? Psrpc { get; set; }

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
    /// Deserializes a YAML string to a LivekitServerConfiguration.
    /// </summary>
    public static LivekitServerConfiguration FromYaml(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<LivekitServerConfiguration>(yaml);
    }
}

/// <summary>
/// Redis configuration for LiveKit clustering.
/// </summary>
public class RedisConfiguration
{
    /// <summary>
    /// Redis server address (host:port).
    /// </summary>
    [YamlMember(Alias = "address")]
    public string? Address { get; set; }

    /// <summary>
    /// Redis database number.
    /// </summary>
    [YamlMember(Alias = "db")]
    public int? Db { get; set; }

    /// <summary>
    /// Redis username.
    /// </summary>
    [YamlMember(Alias = "username")]
    public string? Username { get; set; }

    /// <summary>
    /// Redis password.
    /// </summary>
    [YamlMember(Alias = "password")]
    public string? Password { get; set; }

    /// <summary>
    /// Sentinel master name for Redis Sentinel setup.
    /// </summary>
    [YamlMember(Alias = "sentinel_master_name")]
    public string? SentinelMasterName { get; set; }

    /// <summary>
    /// Sentinel addresses for Redis Sentinel setup.
    /// </summary>
    [YamlMember(Alias = "sentinel_addresses")]
    public List<string>? SentinelAddresses { get; set; }

    /// <summary>
    /// Sentinel username if different credentials needed.
    /// </summary>
    [YamlMember(Alias = "sentinel_username")]
    public string? SentinelUsername { get; set; }

    /// <summary>
    /// Sentinel password if different credentials needed.
    /// </summary>
    [YamlMember(Alias = "sentinel_password")]
    public string? SentinelPassword { get; set; }

    /// <summary>
    /// Cluster addresses for Redis Cluster setup.
    /// </summary>
    [YamlMember(Alias = "cluster_addresses")]
    public List<string>? ClusterAddresses { get; set; }

    /// <summary>
    /// TLS configuration for Redis.
    /// </summary>
    [YamlMember(Alias = "tls")]
    public RedisTlsConfiguration? Tls { get; set; }
}

/// <summary>
/// Redis TLS configuration.
/// </summary>
public class RedisTlsConfiguration
{
    /// <summary>
    /// Enable TLS for Redis connection.
    /// </summary>
    [YamlMember(Alias = "enabled")]
    public bool? Enabled { get; set; }

    /// <summary>
    /// When true, LiveKit will not verify the server's certificate.
    /// </summary>
    [YamlMember(Alias = "insecure")]
    public bool? Insecure { get; set; }

    /// <summary>
    /// Server name for TLS verification.
    /// </summary>
    [YamlMember(Alias = "server_name")]
    public string? ServerName { get; set; }

    /// <summary>
    /// File containing trusted root certificates.
    /// </summary>
    [YamlMember(Alias = "ca_cert_file")]
    public string? CaCertFile { get; set; }

    /// <summary>
    /// Client certificate file path.
    /// </summary>
    [YamlMember(Alias = "client_cert_file")]
    public string? ClientCertFile { get; set; }

    /// <summary>
    /// Client key file path.
    /// </summary>
    [YamlMember(Alias = "client_key_file")]
    public string? ClientKeyFile { get; set; }
}

/// <summary>
/// WebRTC configuration.
/// </summary>
public class RtcConfiguration
{
    /// <summary>
    /// UDP port range start for client traffic.
    /// </summary>
    [YamlMember(Alias = "port_range_start")]
    public int? PortRangeStart { get; set; }

    /// <summary>
    /// UDP port range end for client traffic.
    /// </summary>
    [YamlMember(Alias = "port_range_end")]
    public int? PortRangeEnd { get; set; }

    /// <summary>
    /// TCP port for WebRTC ICE over TCP when UDP isn't available.
    /// </summary>
    [YamlMember(Alias = "tcp_port")]
    public int? TcpPort { get; set; }

    /// <summary>
    /// When true, attempts to discover the host's public IP via STUN.
    /// </summary>
    [YamlMember(Alias = "use_external_ip")]
    public bool? UseExternalIp { get; set; }

    /// <summary>
    /// Public IP of the node (if use_external_ip is false).
    /// </summary>
    [YamlMember(Alias = "node_ip")]
    public string? NodeIp { get; set; }

    /// <summary>
    /// UDP mux port or range (e.g., "7882-7892").
    /// </summary>
    [YamlMember(Alias = "udp_port")]
    public string? UdpPort { get; set; }

    /// <summary>
    /// Use a lite ICE agent for faster connections (may cause issues behind NAT).
    /// </summary>
    [YamlMember(Alias = "use_ice_lite")]
    public bool? UseIceLite { get; set; }

    /// <summary>
    /// Optional STUN servers for clients to use.
    /// </summary>
    [YamlMember(Alias = "stun_servers")]
    public List<string>? StunServers { get; set; }

    /// <summary>
    /// Optional TURN servers for clients.
    /// </summary>
    [YamlMember(Alias = "turn_servers")]
    public List<TurnServerConfiguration>? TurnServers { get; set; }

    /// <summary>
    /// Congestion control configuration.
    /// </summary>
    [YamlMember(Alias = "congestion_control")]
    public CongestionControlConfiguration? CongestionControl { get; set; }

    /// <summary>
    /// Allow automatic connection fallback to TCP and TURN/TLS when UDP is unstable.
    /// </summary>
    [YamlMember(Alias = "allow_tcp_fallback")]
    public bool? AllowTcpFallback { get; set; }

    /// <summary>
    /// Number of packets to buffer in the SFU for video.
    /// </summary>
    [YamlMember(Alias = "packet_buffer_size_video")]
    public int? PacketBufferSizeVideo { get; set; }

    /// <summary>
    /// Number of packets to buffer in the SFU for audio.
    /// </summary>
    [YamlMember(Alias = "packet_buffer_size_audio")]
    public int? PacketBufferSizeAudio { get; set; }

    /// <summary>
    /// PLI throttle configuration.
    /// </summary>
    [YamlMember(Alias = "pli_throttle")]
    public PliThrottleConfiguration? PliThrottle { get; set; }

    /// <summary>
    /// Enable loopback candidate collection.
    /// </summary>
    [YamlMember(Alias = "enable_loopback_candidate")]
    public bool? EnableLoopbackCandidate { get; set; }

    /// <summary>
    /// Network interface filter configuration.
    /// </summary>
    [YamlMember(Alias = "interfaces")]
    public InterfaceFilterConfiguration? Interfaces { get; set; }

    /// <summary>
    /// IP address filter configuration.
    /// </summary>
    [YamlMember(Alias = "ips")]
    public IpFilterConfiguration? Ips { get; set; }

    /// <summary>
    /// Enable mDNS name candidate.
    /// </summary>
    [YamlMember(Alias = "use_mdns")]
    public bool? UseMdns { get; set; }

    /// <summary>
    /// Enable strict ACKs for peer connections.
    /// </summary>
    [YamlMember(Alias = "strict_acks")]
    public bool? StrictAcks { get; set; }

    /// <summary>
    /// Batch I/O configuration.
    /// </summary>
    [YamlMember(Alias = "batch_io")]
    public BatchIoConfiguration? BatchIo { get; set; }

    /// <summary>
    /// Max bytes to buffer for data channel (0 for unlimited).
    /// </summary>
    [YamlMember(Alias = "data_channel_max_buffered_amount")]
    public int? DataChannelMaxBufferedAmount { get; set; }
}

/// <summary>
/// External TURN server configuration.
/// </summary>
public class TurnServerConfiguration
{
    /// <summary>
    /// TURN server host.
    /// </summary>
    [YamlMember(Alias = "host")]
    public string? Host { get; set; }

    /// <summary>
    /// TURN server port.
    /// </summary>
    [YamlMember(Alias = "port")]
    public int? Port { get; set; }

    /// <summary>
    /// Protocol: tls, tcp, or udp.
    /// </summary>
    [YamlMember(Alias = "protocol")]
    public string? Protocol { get; set; }

    /// <summary>
    /// Shared secret for TURN server authentication.
    /// </summary>
    [YamlMember(Alias = "secret")]
    public string? Secret { get; set; }

    /// <summary>
    /// Time to live in seconds.
    /// </summary>
    [YamlMember(Alias = "ttl")]
    public int? Ttl { get; set; }

    /// <summary>
    /// Username for insecure authentication.
    /// </summary>
    [YamlMember(Alias = "username")]
    public string? Username { get; set; }

    /// <summary>
    /// Credential for insecure authentication.
    /// </summary>
    [YamlMember(Alias = "credential")]
    public string? Credential { get; set; }
}

/// <summary>
/// Congestion control configuration.
/// </summary>
public class CongestionControlConfiguration
{
    /// <summary>
    /// Enable congestion control.
    /// </summary>
    [YamlMember(Alias = "enabled")]
    public bool? Enabled { get; set; }

    /// <summary>
    /// Allow pausing tracks during congestion.
    /// </summary>
    [YamlMember(Alias = "allow_pause")]
    public bool? AllowPause { get; set; }
}

/// <summary>
/// PLI throttle configuration.
/// </summary>
public class PliThrottleConfiguration
{
    /// <summary>
    /// Low quality PLI throttle.
    /// </summary>
    [YamlMember(Alias = "low_quality")]
    public string? LowQuality { get; set; }

    /// <summary>
    /// Mid quality PLI throttle.
    /// </summary>
    [YamlMember(Alias = "mid_quality")]
    public string? MidQuality { get; set; }

    /// <summary>
    /// High quality PLI throttle.
    /// </summary>
    [YamlMember(Alias = "high_quality")]
    public string? HighQuality { get; set; }
}

/// <summary>
/// Network interface filter configuration.
/// </summary>
public class InterfaceFilterConfiguration
{
    /// <summary>
    /// Interfaces to include.
    /// </summary>
    [YamlMember(Alias = "includes")]
    public List<string>? Includes { get; set; }

    /// <summary>
    /// Interfaces to exclude.
    /// </summary>
    [YamlMember(Alias = "excludes")]
    public List<string>? Excludes { get; set; }
}

/// <summary>
/// IP address filter configuration.
/// </summary>
public class IpFilterConfiguration
{
    /// <summary>
    /// IP CIDRs to include.
    /// </summary>
    [YamlMember(Alias = "includes")]
    public List<string>? Includes { get; set; }

    /// <summary>
    /// IP CIDRs to exclude.
    /// </summary>
    [YamlMember(Alias = "excludes")]
    public List<string>? Excludes { get; set; }
}

/// <summary>
/// Batch I/O configuration.
/// </summary>
public class BatchIoConfiguration
{
    /// <summary>
    /// Batch size for I/O operations.
    /// </summary>
    [YamlMember(Alias = "batch_size")]
    public int? BatchSize { get; set; }

    /// <summary>
    /// Maximum flush interval.
    /// </summary>
    [YamlMember(Alias = "max_flush_interval")]
    public string? MaxFlushInterval { get; set; }
}

/// <summary>
/// Logging configuration.
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// Log level: debug, info, warn, error.
    /// </summary>
    [YamlMember(Alias = "level")]
    public string? Level { get; set; }

    /// <summary>
    /// Log level for pion (default: error).
    /// </summary>
    [YamlMember(Alias = "pion_level")]
    public string? PionLevel { get; set; }

    /// <summary>
    /// Emit JSON formatted logs.
    /// </summary>
    [YamlMember(Alias = "json")]
    public bool? Json { get; set; }

    /// <summary>
    /// Enable log sampling for production.
    /// </summary>
    [YamlMember(Alias = "sample")]
    public bool? Sample { get; set; }
}

/// <summary>
/// Default room configuration.
/// </summary>
public class RoomConfiguration
{
    /// <summary>
    /// Allow rooms to be automatically created when participants join.
    /// </summary>
    [YamlMember(Alias = "auto_create")]
    public bool? AutoCreate { get; set; }

    /// <summary>
    /// Seconds to keep the room open if no one joins.
    /// </summary>
    [YamlMember(Alias = "empty_timeout")]
    public int? EmptyTimeout { get; set; }

    /// <summary>
    /// Seconds to keep the room open after everyone leaves.
    /// </summary>
    [YamlMember(Alias = "departure_timeout")]
    public int? DepartureTimeout { get; set; }

    /// <summary>
    /// Maximum number of participants (0 for no limit).
    /// </summary>
    [YamlMember(Alias = "max_participants")]
    public int? MaxParticipants { get; set; }

    /// <summary>
    /// Enabled codecs for the room.
    /// </summary>
    [YamlMember(Alias = "enabled_codecs")]
    public List<CodecConfiguration>? EnabledCodecs { get; set; }

    /// <summary>
    /// Allow tracks to be unmuted remotely.
    /// </summary>
    [YamlMember(Alias = "enable_remote_unmute")]
    public bool? EnableRemoteUnmute { get; set; }

    /// <summary>
    /// Playout delay configuration.
    /// </summary>
    [YamlMember(Alias = "playout_delay")]
    public PlayoutDelayConfiguration? PlayoutDelay { get; set; }

    /// <summary>
    /// Enable stream synchronization.
    /// </summary>
    [YamlMember(Alias = "sync_streams")]
    public bool? SyncStreams { get; set; }
}

/// <summary>
/// Codec configuration.
/// </summary>
public class CodecConfiguration
{
    /// <summary>
    /// MIME type (e.g., "audio/opus", "video/vp8").
    /// </summary>
    [YamlMember(Alias = "mime")]
    public string? Mime { get; set; }
}

/// <summary>
/// Playout delay configuration.
/// </summary>
public class PlayoutDelayConfiguration
{
    /// <summary>
    /// Enable playout delay.
    /// </summary>
    [YamlMember(Alias = "enabled")]
    public bool? Enabled { get; set; }

    /// <summary>
    /// Minimum playout delay in ms.
    /// </summary>
    [YamlMember(Alias = "min")]
    public int? Min { get; set; }

    /// <summary>
    /// Maximum playout delay in ms.
    /// </summary>
    [YamlMember(Alias = "max")]
    public int? Max { get; set; }
}

/// <summary>
/// Webhook configuration.
/// </summary>
public class WebhookConfiguration
{
    /// <summary>
    /// API key to use for signing webhook messages.
    /// </summary>
    [YamlMember(Alias = "api_key")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// URLs to be notified of room events.
    /// </summary>
    [YamlMember(Alias = "urls")]
    public List<string>? Urls { get; set; }
}

/// <summary>
/// Embedded TURN server configuration.
/// </summary>
public class TurnConfiguration
{
    /// <summary>
    /// Enable embedded TURN server.
    /// </summary>
    [YamlMember(Alias = "enabled")]
    public bool? Enabled { get; set; }

    /// <summary>
    /// UDP port (recommended 443 if not running HTTP3/QUIC).
    /// </summary>
    [YamlMember(Alias = "udp_port")]
    public int? UdpPort { get; set; }

    /// <summary>
    /// TLS port (must be 443 if not using a load balancer).
    /// </summary>
    [YamlMember(Alias = "tls_port")]
    public int? TlsPort { get; set; }

    /// <summary>
    /// TURN relay port range start.
    /// </summary>
    [YamlMember(Alias = "relay_range_start")]
    public int? RelayRangeStart { get; set; }

    /// <summary>
    /// TURN relay port range end.
    /// </summary>
    [YamlMember(Alias = "relay_range_end")]
    public int? RelayRangeEnd { get; set; }

    /// <summary>
    /// Use external TLS termination.
    /// </summary>
    [YamlMember(Alias = "external_tls")]
    public bool? ExternalTls { get; set; }

    /// <summary>
    /// Domain name (must match TLS cert).
    /// </summary>
    [YamlMember(Alias = "domain")]
    public string? Domain { get; set; }

    /// <summary>
    /// Certificate file path.
    /// </summary>
    [YamlMember(Alias = "cert_file")]
    public string? CertFile { get; set; }

    /// <summary>
    /// Key file path.
    /// </summary>
    [YamlMember(Alias = "key_file")]
    public string? KeyFile { get; set; }
}

/// <summary>
/// Ingress configuration.
/// </summary>
public class IngressConfiguration
{
    /// <summary>
    /// RTMP base URL prefix for RTMP ingress.
    /// </summary>
    [YamlMember(Alias = "rtmp_base_url")]
    public string? RtmpBaseUrl { get; set; }

    /// <summary>
    /// WHIP base URL prefix for WHIP ingress.
    /// </summary>
    [YamlMember(Alias = "whip_base_url")]
    public string? WhipBaseUrl { get; set; }
}

/// <summary>
/// Node selector configuration.
/// </summary>
public class NodeSelectorConfiguration
{
    /// <summary>
    /// Node selection kind: any, sysload, cpuload, regionaware.
    /// </summary>
    [YamlMember(Alias = "kind")]
    public string? Kind { get; set; }

    /// <summary>
    /// Sort priority: random, sysload, cpuload, rooms, clients, tracks, bytespersec.
    /// </summary>
    [YamlMember(Alias = "sort_by")]
    public string? SortBy { get; set; }

    /// <summary>
    /// Selection algorithm: lowest, twochoice.
    /// </summary>
    [YamlMember(Alias = "algorithm")]
    public string? Algorithm { get; set; }

    /// <summary>
    /// System load limit (0-1.0).
    /// </summary>
    [YamlMember(Alias = "sysload_limit")]
    public double? SysloadLimit { get; set; }

    /// <summary>
    /// Region configurations.
    /// </summary>
    [YamlMember(Alias = "regions")]
    public List<RegionConfiguration>? Regions { get; set; }
}

/// <summary>
/// Region configuration.
/// </summary>
public class RegionConfiguration
{
    /// <summary>
    /// Region name.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Latitude coordinate.
    /// </summary>
    [YamlMember(Alias = "lat")]
    public double? Lat { get; set; }

    /// <summary>
    /// Longitude coordinate.
    /// </summary>
    [YamlMember(Alias = "lon")]
    public double? Lon { get; set; }
}

/// <summary>
/// Node limits configuration.
/// </summary>
public class LimitConfiguration
{
    /// <summary>
    /// Maximum number of tracks (-1 to disable).
    /// </summary>
    [YamlMember(Alias = "num_tracks")]
    public int? NumTracks { get; set; }

    /// <summary>
    /// Maximum bytes per second.
    /// </summary>
    [YamlMember(Alias = "bytes_per_sec")]
    public long? BytesPerSec { get; set; }

    /// <summary>
    /// Video subscription limit per participant.
    /// </summary>
    [YamlMember(Alias = "subscription_limit_video")]
    public int? SubscriptionLimitVideo { get; set; }

    /// <summary>
    /// Audio subscription limit per participant.
    /// </summary>
    [YamlMember(Alias = "subscription_limit_audio")]
    public int? SubscriptionLimitAudio { get; set; }

    /// <summary>
    /// Maximum metadata size (0 for no limit).
    /// </summary>
    [YamlMember(Alias = "max_metadata_size")]
    public int? MaxMetadataSize { get; set; }

    /// <summary>
    /// Maximum attributes size (0 for no limit).
    /// </summary>
    [YamlMember(Alias = "max_attributes_size")]
    public int? MaxAttributesSize { get; set; }

    /// <summary>
    /// Maximum room name length.
    /// </summary>
    [YamlMember(Alias = "max_room_name_length")]
    public int? MaxRoomNameLength { get; set; }

    /// <summary>
    /// Maximum participant identity length.
    /// </summary>
    [YamlMember(Alias = "max_participant_identity_length")]
    public int? MaxParticipantIdentityLength { get; set; }
}

/// <summary>
/// Audio level sensitivity configuration.
/// </summary>
public class AudioConfiguration
{
    /// <summary>
    /// Minimum level to be considered active (0-127, where 0 is loudest).
    /// </summary>
    [YamlMember(Alias = "active_level")]
    public int? ActiveLevel { get; set; }

    /// <summary>
    /// Percentile threshold for active detection (0-100).
    /// </summary>
    [YamlMember(Alias = "min_percentile")]
    public int? MinPercentile { get; set; }

    /// <summary>
    /// Update interval in ms.
    /// </summary>
    [YamlMember(Alias = "update_interval")]
    public int? UpdateInterval { get; set; }

    /// <summary>
    /// Number of samples for smoothing.
    /// </summary>
    [YamlMember(Alias = "smooth_intervals")]
    public int? SmoothIntervals { get; set; }

    /// <summary>
    /// Enable RED encoding for opus audio.
    /// </summary>
    [YamlMember(Alias = "active_red_encoding")]
    public bool? ActiveRedEncoding { get; set; }
}

/// <summary>
/// Signal relay configuration.
/// </summary>
public class SignalRelayConfiguration
{
    /// <summary>
    /// Retry timeout for message delivery.
    /// </summary>
    [YamlMember(Alias = "retry_timeout")]
    public string? RetryTimeout { get; set; }

    /// <summary>
    /// Minimum retry interval.
    /// </summary>
    [YamlMember(Alias = "min_retry_interval")]
    public string? MinRetryInterval { get; set; }

    /// <summary>
    /// Maximum retry interval.
    /// </summary>
    [YamlMember(Alias = "max_retry_interval")]
    public string? MaxRetryInterval { get; set; }

    /// <summary>
    /// Stream buffer size.
    /// </summary>
    [YamlMember(Alias = "stream_buffer_size")]
    public int? StreamBufferSize { get; set; }
}

/// <summary>
/// PSRPC configuration.
/// </summary>
public class PsrpcConfiguration
{
    /// <summary>
    /// Maximum number of RPC attempts.
    /// </summary>
    [YamlMember(Alias = "max_attempts")]
    public int? MaxAttempts { get; set; }

    /// <summary>
    /// Initial timeout for calls.
    /// </summary>
    [YamlMember(Alias = "timeout")]
    public string? Timeout { get; set; }

    /// <summary>
    /// Backoff time added after each failure.
    /// </summary>
    [YamlMember(Alias = "backoff")]
    public string? Backoff { get; set; }

    /// <summary>
    /// Buffer size for messages.
    /// </summary>
    [YamlMember(Alias = "buffer_size")]
    public int? BufferSize { get; set; }
}
