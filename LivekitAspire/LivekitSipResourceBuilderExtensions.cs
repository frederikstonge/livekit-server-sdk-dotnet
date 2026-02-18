using Aspire.Hosting.ApplicationModel;

// Put extensions in the Aspire.Hosting namespace to ease discovery as referencing
// the Aspire hosting package automatically adds this namespace.
namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding LiveKit resources to the distributed application builder.
/// </summary>
public static class LivekitSipResourceBuilderExtensions
{
    /// <summary>
    /// Adds a LiveKit SIP service to the application for telephony integration.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the SIP resource.</param>
    /// <param name="livekitServer">The LiveKit server resource.</param>
    /// <param name="redis">Optional Redis resource for SIP coordination. Required for production use.</param>
    /// <param name="sipPort">The SIP port. Defaults to 5060.</param>
    /// <param name="imageTag">The container image tag. Defaults to "latest".</param>
    /// <returns>An <see cref="IResourceBuilder{LivekitSipResource}"/> for the SIP service.</returns>
    public static IResourceBuilder<LivekitSipResource> AddLivekitSip(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<LivekitServerResource> livekitServer,
        IResourceBuilder<IResource>? redis = null,
        int? sipPort = null,
        string imageTag = "latest")
    {
        ArgumentNullException.ThrowIfNull(livekitServer);

        var resource = new LivekitSipResource(name)
        {
            ServerResource = livekitServer,
            RedisResource = redis
        };

        var sipBuilder = builder.AddResource(resource)
            .WithImage(LivekitContainerImageTags.SipImage)
            .WithImageRegistry(LivekitContainerImageTags.Registry)
            .WithImageTag(imageTag)
            .WithEndpoint(targetPort: 5060, port: sipPort, name: LivekitSipResource.SipEndpointName, scheme: "udp")
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables["SIP_API_KEY"] = livekitServer.Resource.ApiKeyParameter!.Resource;
                context.EnvironmentVariables["SIP_API_SECRET"] = livekitServer.Resource.ApiSecretParameter!.Resource;
                context.EnvironmentVariables["SIP_WS_URL"] = livekitServer.Resource.ConnectionStringExpression;
                context.EnvironmentVariables["SIP_PORT"] = "5060";
                context.EnvironmentVariables["SIP_RTP_PORT"] = "10000-20000";
            })
            .WaitFor(livekitServer);

        if (redis != null)
        {
            sipBuilder = sipBuilder
                .WithEnvironment(context =>
                {
                    if (redis.Resource is IResourceWithConnectionString redisWithConnectionString)
                    {
                        context.EnvironmentVariables["SIP_REDIS_ADDRESS"] =
                            redisWithConnectionString.ConnectionStringExpression;
                    }
                })
                .WaitFor(redis);
        }

        return sipBuilder;
    }
}
