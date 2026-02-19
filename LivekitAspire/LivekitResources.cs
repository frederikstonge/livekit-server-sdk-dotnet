using LivekitAspire;

// For ease of discovery, resource types should be placed in
// the Aspire.Hosting.ApplicationModel namespace.
namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a LiveKit server container resource.
/// LiveKit server is the core WebRTC SFU that handles realtime communication.
/// </summary>
public sealed class LivekitServerResource(string name)
    : ContainerResource(name), IResourceWithConnectionString
{
    internal const string HttpEndpointName = "http";
    internal const string RtcTcpEndpointName = "rtc-tcp";

    /// <summary>
    /// API key parameter for authentication.
    /// </summary>
    public IResourceBuilder<ParameterResource>? ApiKeyParameter { get; internal set; }

    /// <summary>
    /// API secret parameter for authentication.
    /// </summary>
    public IResourceBuilder<ParameterResource>? ApiSecretParameter { get; internal set; }

    /// <summary>
    /// Indicates whether dev mode is enabled.
    /// When true, default credentials will be used for referencing resources.
    /// </summary>
    public bool DevModeEnabled { get; internal set; }

    /// <summary>
    /// Optional Redis resource for clustering/scaling.
    /// </summary>
    public IResourceBuilder<IResource>? RedisResource { get; internal set; }

    /// <summary>
    /// The LiveKit server configuration that will be serialized to YAML.
    /// </summary>
    public LivekitServerConfiguration Configuration { get; } = new();

    private EndpointReference? _httpReference;

    /// <summary>
    /// Gets the HTTP endpoint reference for the LiveKit server.
    /// </summary>
    public EndpointReference HttpEndpoint =>
        _httpReference ??= new(this, HttpEndpointName);

    /// <summary>
    /// Connection string expression that returns the WebSocket URL for client connections.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"ws://{HttpEndpoint.Property(EndpointProperty.Host)}:{HttpEndpoint.Property(EndpointProperty.Port)}"
        );
}

/// <summary>
/// Represents a LiveKit Egress container resource.
/// Egress handles recording sessions and streaming output to external destinations.
/// </summary>
public sealed class LivekitEgressResource(string name)
    : ContainerResource(name)
{
    internal const string HttpEndpointName = "http";

    /// <summary>
    /// Reference to the parent LiveKit server resource.
    /// </summary>
    public IResourceBuilder<LivekitServerResource>? ServerResource { get; internal set; }

    /// <summary>
    /// Optional Redis resource for coordination.
    /// </summary>
    public IResourceBuilder<IResource>? RedisResource { get; internal set; }

    /// <summary>
    /// The LiveKit egress configuration that will be serialized to YAML.
    /// </summary>
    public LivekitEgressConfiguration Configuration { get; } = new();
}

/// <summary>
/// Represents a LiveKit Ingress container resource.
/// Ingress enables streaming input via RTMP, WHIP, or URL pull.
/// </summary>
public sealed class LivekitIngressResource(string name)
    : ContainerResource(name)
{
    internal const string HttpEndpointName = "http";
    internal const string RtmpEndpointName = "rtmp";
    internal const string WhipEndpointName = "whip";
    internal const string HttpRelayEndpointName = "http-relay";

    /// <summary>
    /// Reference to the parent LiveKit server resource.
    /// </summary>
    public IResourceBuilder<LivekitServerResource>? ServerResource { get; internal set; }

    /// <summary>
    /// Optional Redis resource for coordination.
    /// </summary>
    public IResourceBuilder<IResource>? RedisResource { get; internal set; }

    /// <summary>
    /// The LiveKit ingress configuration that will be serialized to YAML.
    /// </summary>
    public LivekitIngressConfiguration Configuration { get; } = new();

    private EndpointReference? _rtmpReference;
    private EndpointReference? _whipReference;

    /// <summary>
    /// Gets the RTMP endpoint reference for streaming input.
    /// </summary>
    public EndpointReference RtmpEndpoint =>
        _rtmpReference ??= new(this, RtmpEndpointName);

    /// <summary>
    /// Gets the WHIP endpoint reference for WebRTC streaming input.
    /// </summary>
    public EndpointReference WhipEndpoint =>
        _whipReference ??= new(this, WhipEndpointName);
}

/// <summary>
/// Represents a LiveKit SIP container resource.
/// SIP service enables telephony integration with SIP/PSTN networks.
/// </summary>
public sealed class LivekitSipResource(string name)
    : ContainerResource(name)
{
    internal const string SipEndpointName = "sip";

    /// <summary>
    /// Reference to the parent LiveKit server resource.
    /// </summary>
    public IResourceBuilder<LivekitServerResource>? ServerResource { get; internal set; }

    /// <summary>
    /// Optional Redis resource for coordination.
    /// </summary>
    public IResourceBuilder<IResource>? RedisResource { get; internal set; }

    /// <summary>
    /// The LiveKit SIP configuration that will be serialized to YAML.
    /// </summary>
    public LivekitSipConfiguration Configuration { get; } = new();

    private EndpointReference? _sipReference;

    /// <summary>
    /// Gets the SIP endpoint reference for telephony.
    /// </summary>
    public EndpointReference SipEndpoint =>
        _sipReference ??= new(this, SipEndpointName);
}
