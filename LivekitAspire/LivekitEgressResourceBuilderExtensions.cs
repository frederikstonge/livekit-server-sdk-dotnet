using Aspire.Hosting.ApplicationModel;

// Put extensions in the Aspire.Hosting namespace to ease discovery as referencing
// the Aspire hosting package automatically adds this namespace.
namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding LiveKit resources to the distributed application builder.
/// </summary>
public static class LivekitEgressResourceBuilderExtensions
{
    /// <summary>
    /// Adds a LiveKit Egress service to the application for recording and streaming output.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the egress resource.</param>
    /// <param name="livekitServer">The LiveKit server resource.</param>
    /// <param name="redis">Optional Redis resource for egress coordination. Required for production use.</param>
    /// <param name="imageTag">The container image tag. Defaults to "latest".</param>
    /// <returns>An <see cref="IResourceBuilder{LivekitEgressResource}"/> for the egress service.</returns>
    public static IResourceBuilder<LivekitEgressResource> AddLivekitEgress(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<LivekitServerResource> livekitServer,
        IResourceBuilder<IResource>? redis = null,
        string imageTag = "latest")
    {
        ArgumentNullException.ThrowIfNull(livekitServer);

        var resource = new LivekitEgressResource(name)
        {
            ServerResource = livekitServer,
            RedisResource = redis
        };

        var egressBuilder = builder.AddResource(resource)
            .WithImage(LivekitContainerImageTags.EgressImage)
            .WithImageRegistry(LivekitContainerImageTags.Registry)
            .WithImageTag(imageTag)
            .WithHttpEndpoint(targetPort: 9091, name: LivekitEgressResource.HttpEndpointName)
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables["EGRESS_API_KEY"] = livekitServer.Resource.ApiKeyParameter!.Resource;
                context.EnvironmentVariables["EGRESS_API_SECRET"] = livekitServer.Resource.ApiSecretParameter!.Resource;
                context.EnvironmentVariables["EGRESS_WS_URL"] = livekitServer.Resource.ConnectionStringExpression;
                context.EnvironmentVariables["EGRESS_INSECURE"] = "true";
                context.EnvironmentVariables["EGRESS_HEALTH_PORT"] = "9091";
            })
            .WithHttpHealthCheck("/")
            .WaitFor(livekitServer);

        if (redis != null)
        {
            egressBuilder = egressBuilder
                .WithEnvironment(context =>
                {
                    if (redis.Resource is IResourceWithConnectionString redisWithConnectionString)
                    {
                        context.EnvironmentVariables["EGRESS_REDIS_ADDRESS"] =
                            redisWithConnectionString.ConnectionStringExpression;
                    }
                })
                .WaitFor(redis);
        }

        return egressBuilder;
    }

    /// <summary>
    /// Configures file output settings for egress recordings.
    /// </summary>
    /// <param name="builder">The egress resource builder.</param>
    /// <param name="outputPath">The path where recordings will be stored.</param>
    /// <returns>The resource builder for method chaining.</returns>
    public static IResourceBuilder<LivekitEgressResource> WithFileOutput(
        this IResourceBuilder<LivekitEgressResource> builder,
        string outputPath = "/out")
    {
        return builder
            .WithEnvironment("EGRESS_FILE_OUTPUT_TYPE_LOCAL_OUTPUT_DIRECTORY", outputPath)
            .WithBindMount(outputPath, outputPath);
    }
}
