using Aspire.Hosting.ApplicationModel;
using LivekitAspire;
using Microsoft.Extensions.Hosting;

// Put extensions in the Aspire.Hosting namespace to ease discovery as referencing
// the Aspire hosting package automatically adds this namespace.
namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the LiveKit CLI resource to the distributed application builder.
/// </summary>
public static class LivekitCliResourceBuilderExtensions
{
    /// <summary>
    /// Adds a LiveKit CLI container resource to the application.
    /// The CLI provides utilities for interacting with LiveKit servers, including room management,
    /// token generation, load testing, and administrative tasks.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the CLI resource.</param>
    /// <param name="livekitServer">The LiveKit server resource.</param>
    /// <param name="imageTag">The container image tag. Defaults to "latest".</param>
    /// <returns>An <see cref="IResourceBuilder{LivekitCliResource}"/> for the CLI.</returns>
    /// <remarks>
    /// <para>
    /// The CLI container is configured with environment variables for server URL and credentials.
    /// You can run CLI commands using docker exec or by specifying arguments with <see cref="WithArgs"/>.
    /// </para>
    /// <para>
    /// Example commands that can be run:
    /// <list type="bullet">
    /// <item><description>lk room list - List all rooms</description></item>
    /// <item><description>lk room create &lt;room-name&gt; - Create a room</description></item>
    /// <item><description>lk token create --room &lt;room&gt; --identity &lt;identity&gt; - Generate a token</description></item>
    /// <item><description>lk load-test - Run load tests</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static IResourceBuilder<LivekitCliResource> RunLivekitCliCommand(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<LivekitServerResource> livekitServer,
        string[] args,
        string imageTag = "latest")
    {
        ArgumentNullException.ThrowIfNull(livekitServer);

        var resource = new LivekitCliResource(name)
        {
            ServerResource = livekitServer
        };

        // Register a callback to resolve deferred configuration
        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(resource, async (@event, ct) =>
        {
            // Set API credentials from server resource
            if (livekitServer.Resource.ApiKeyParameter != null)
            {
                resource.Configuration.ApiKey = await livekitServer.Resource.ApiKeyParameter.Resource.GetValueAsync(ct) ?? "";
            }
            if (livekitServer.Resource.ApiSecretParameter != null)
            {
                resource.Configuration.ApiSecret = await livekitServer.Resource.ApiSecretParameter.Resource.GetValueAsync(ct) ?? "";
            }

            // Set URL from server resource
            var serverEndpoint = livekitServer.Resource.HttpEndpoint;
            resource.Configuration.Url = $"ws://{serverEndpoint.Property(EndpointProperty.Host)}:{serverEndpoint.Property(EndpointProperty.Port)}";

            // Enable insecure for development
            resource.Configuration.Insecure = builder.Environment.IsDevelopment();
        });

        var cliBuilder = builder.AddResource(resource)
            .WithImage(LivekitContainerImageTags.CliImage)
            .WithImageRegistry(LivekitContainerImageTags.Registry)
            .WithImageTag(imageTag)
            .WithEnvironment(context =>
            {
                // The CLI reads configuration from environment variables
                if (!string.IsNullOrEmpty(resource.Configuration.Url))
                {
                    context.EnvironmentVariables["LIVEKIT_URL"] = resource.Configuration.Url;
                }
                if (!string.IsNullOrEmpty(resource.Configuration.ApiKey))
                {
                    context.EnvironmentVariables["LIVEKIT_API_KEY"] = resource.Configuration.ApiKey;
                }
                if (!string.IsNullOrEmpty(resource.Configuration.ApiSecret))
                {
                    context.EnvironmentVariables["LIVEKIT_API_SECRET"] = resource.Configuration.ApiSecret;
                }
                if (!string.IsNullOrEmpty(resource.Configuration.Project))
                {
                    context.EnvironmentVariables["LIVEKIT_PROJECT"] = resource.Configuration.Project;
                }
                if (resource.Configuration.Insecure == true)
                {
                    context.EnvironmentVariables["LIVEKIT_INSECURE"] = "true";
                }
                if (resource.Configuration.Verbose == true)
                {
                    context.EnvironmentVariables["LIVEKIT_VERBOSE"] = "true";
                }
            })
            .WithArgs(args)
            .WaitFor(livekitServer);

        return cliBuilder;
    }

    /// <summary>
    /// Configures the LiveKit CLI using a configuration object.
    /// </summary>
    /// <param name="builder">The CLI resource builder.</param>
    /// <param name="configure">An action to configure the LiveKit CLI settings.</param>
    /// <returns>The resource builder for method chaining.</returns>
    public static IResourceBuilder<LivekitCliResource> WithConfiguration(
        this IResourceBuilder<LivekitCliResource> builder,
        Action<LivekitCliConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(builder.Resource.Configuration);
        return builder;
    }

    /// <summary>
    /// Sets the project name for the LiveKit CLI.
    /// </summary>
    /// <param name="builder">The CLI resource builder.</param>
    /// <param name="project">The project name.</param>
    /// <returns>The resource builder for method chaining.</returns>
    public static IResourceBuilder<LivekitCliResource> WithProject(
        this IResourceBuilder<LivekitCliResource> builder,
        string project)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(project);
        builder.Resource.Configuration.Project = project;
        return builder;
    }

    /// <summary>
    /// Enables verbose logging for the LiveKit CLI.
    /// </summary>
    /// <param name="builder">The CLI resource builder.</param>
    /// <param name="verbose">Whether to enable verbose logging. Defaults to true.</param>
    /// <returns>The resource builder for method chaining.</returns>
    public static IResourceBuilder<LivekitCliResource> WithVerbose(
        this IResourceBuilder<LivekitCliResource> builder,
        bool verbose = true)
    {
        builder.Resource.Configuration.Verbose = verbose;
        return builder;
    }
}
