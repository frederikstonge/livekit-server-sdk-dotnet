using Aspire.Hosting.ApplicationModel;

// Put extensions in the Aspire.Hosting namespace to ease discovery as referencing
// the Aspire hosting package automatically adds this namespace.
namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding LiveKit resources to the distributed application builder.
/// </summary>
public static class LivekitIngressResourceBuilderExtensions
{
    /// <summary>
    /// Adds a LiveKit Ingress service to the application for streaming input (RTMP, WHIP).
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the ingress resource.</param>
    /// <param name="livekitServer">The LiveKit server resource.</param>
    /// <param name="redis">Optional Redis resource for ingress coordination. Required for production use.</param>
    /// <param name="rtmpPort">The RTMP port. Defaults to 1935.</param>
    /// <param name="whipPort">The WHIP port. Defaults to 8085.</param>
    /// <param name="imageTag">The container image tag. Defaults to "latest".</param>
    /// <returns>An <see cref="IResourceBuilder{LivekitIngressResource}"/> for the ingress service.</returns>
    public static IResourceBuilder<LivekitIngressResource> AddLivekitIngress(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<LivekitServerResource> livekitServer,
        IResourceBuilder<IResource>? redis = null,
        int? rtmpPort = null,
        int? whipPort = null,
        string imageTag = "latest")
    {
        ArgumentNullException.ThrowIfNull(livekitServer);

        var resource = new LivekitIngressResource(name)
        {
            ServerResource = livekitServer,
            RedisResource = redis
        };

        var ingressBuilder = builder.AddResource(resource)
            .WithImage(LivekitContainerImageTags.IngressImage)
            .WithImageRegistry(LivekitContainerImageTags.Registry)
            .WithImageTag(imageTag)
            .WithHttpEndpoint(targetPort: 9091, name: LivekitIngressResource.HttpEndpointName)
            .WithEndpoint(targetPort: 1935, port: rtmpPort, name: LivekitIngressResource.RtmpEndpointName, scheme: "tcp")
            .WithEndpoint(targetPort: 8085, port: whipPort, name: LivekitIngressResource.WhipEndpointName)
            .WithEndpoint(targetPort: 9090, name: LivekitIngressResource.HttpRelayEndpointName)
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables["INGRESS_API_KEY"] = livekitServer.Resource.ApiKeyParameter!.Resource;
                context.EnvironmentVariables["INGRESS_API_SECRET"] = livekitServer.Resource.ApiSecretParameter!.Resource;
                context.EnvironmentVariables["INGRESS_WS_URL"] = livekitServer.Resource.ConnectionStringExpression;
                context.EnvironmentVariables["INGRESS_RTMP_PORT"] = "1935";
                context.EnvironmentVariables["INGRESS_WHIP_PORT"] = "8085";
                context.EnvironmentVariables["INGRESS_HTTP_RELAY_PORT"] = "9090";
                context.EnvironmentVariables["INGRESS_HEALTH_PORT"] = "9091";
            })
            .WithHttpHealthCheck("/")
            .WaitFor(livekitServer);

        if (redis != null)
        {
            ingressBuilder = ingressBuilder
                .WithEnvironment(context =>
                {
                    if (redis.Resource is IResourceWithConnectionString redisWithConnectionString)
                    {
                        context.EnvironmentVariables["INGRESS_REDIS_ADDRESS"] =
                            redisWithConnectionString.ConnectionStringExpression;
                    }
                })
                .WaitFor(redis);
        }

        return ingressBuilder;
    }
}
