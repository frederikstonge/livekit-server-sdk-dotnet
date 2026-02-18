namespace Aspire.Hosting;

/// <summary>
/// Container image tags for LiveKit services.
/// </summary>
internal static class LivekitContainerImageTags
{
    internal const string Registry = "docker.io";
    internal const string ServerImage = "livekit/livekit-server";
    internal const string EgressImage = "livekit/egress";
    internal const string IngressImage = "livekit/ingress";
    internal const string SipImage = "livekit/sip";
}