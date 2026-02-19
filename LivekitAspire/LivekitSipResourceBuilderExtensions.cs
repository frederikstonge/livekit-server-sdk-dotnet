using Aspire.Hosting.ApplicationModel;
using LivekitAspire;
using Microsoft.Extensions.Hosting;

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
    /// <param name="rtpPortRange">The RTP port range. Defaults to "10000-20000".</param>
    /// <param name="imageTag">The container image tag. Defaults to "latest".</param>
    /// <returns>An <see cref="IResourceBuilder{LivekitSipResource}"/> for the SIP service.</returns>
    public static IResourceBuilder<LivekitSipResource> AddLivekitSip(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<LivekitServerResource> livekitServer,
        IResourceBuilder<IResource>? redis = null,
        int sipPort = 5060,
        string rtpPortRange = "10000-20000",
        string imageTag = "latest")
    {
        ArgumentNullException.ThrowIfNull(livekitServer);

        var resource = new LivekitSipResource(name)
        {
            ServerResource = livekitServer,
            RedisResource = redis
        };

        // Register a callback to resolve deferred configuration and write the YAML file
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

            // Set WebSocket URL from server resource
            var serverEndpoint = livekitServer.Resource.HttpEndpoint;
            resource.Configuration.WsUrl = $"ws://{serverEndpoint.Property(EndpointProperty.Host)}:{serverEndpoint.Property(EndpointProperty.Port)}";
            
            // Configure SIP and RTP ports
            resource.Configuration.SipPort = sipPort;
            resource.Configuration.RtpPort = rtpPortRange;

            // Enable insecure for development
            resource.Configuration.Insecure = builder.Environment.IsDevelopment();

            // Resolve Redis configuration if a Redis resource is attached
            if (resource.RedisResource != null)
            {
                var redisResource = resource.RedisResource.Resource;

                // For container-to-container communication, we need the internal container network address
                if (redisResource is IResourceWithEndpoints redisWithEndpoints)
                {
                    var endpoint = redisWithEndpoints.GetEndpoint("tcp");
                    var redisHost = redisResource.Name;
                    var redisPort = endpoint.TargetPort;

                    resource.Configuration.Redis ??= new RedisConfiguration();
                    resource.Configuration.Redis.Address = $"{redisHost}:{redisPort}";
                }

                // Get password from connection string if available
                if (redisResource is IResourceWithConnectionString redisWithConnectionString)
                {
                    var connectionString = await redisWithConnectionString.GetConnectionStringAsync(ct);
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        var (password, sslEnabled) = ParseRedisConnectionString(connectionString);

                        if (!string.IsNullOrEmpty(password))
                        {
                            resource.Configuration.Redis ??= new RedisConfiguration();
                            resource.Configuration.Redis.Password = password;
                        }

                        if (sslEnabled == true)
                        {
                            resource.Configuration.Redis ??= new RedisConfiguration();
                            resource.Configuration.Redis.Tls ??= new RedisTlsConfiguration();
                            resource.Configuration.Redis.Tls.Enabled = true;
                            resource.Configuration.Redis.Tls.Insecure = builder.Environment.IsDevelopment();
                        }
                    }
                }
            }
        });

        var sipBuilder = builder.AddResource(resource)
            .WithImage(LivekitContainerImageTags.SipImage)
            .WithImageRegistry(LivekitContainerImageTags.Registry)
            .WithImageTag(imageTag)
            .WithEndpoint(targetPort: sipPort, port: sipPort, name: LivekitSipResource.SipEndpointName, scheme: "udp")
            .WithEnvironment(context =>
            {
                // SIP service uses SIP_CONFIG_BODY environment variable for configuration
                context.EnvironmentVariables["SIP_CONFIG_BODY"] = resource.Configuration.ToYaml();
            })
            .WaitFor(livekitServer);

        if (redis != null)
        {
            sipBuilder = sipBuilder.WaitFor(redis);
        }

        return sipBuilder;
    }

    /// <summary>
    /// Configures the LiveKit SIP using a configuration object that will be serialized to YAML.
    /// </summary>
    /// <param name="builder">The SIP resource builder.</param>
    /// <param name="configure">An action to configure the LiveKit SIP settings.</param>
    /// <returns>The resource builder for method chaining.</returns>
    public static IResourceBuilder<LivekitSipResource> WithConfiguration(
        this IResourceBuilder<LivekitSipResource> builder,
        Action<LivekitSipConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(builder.Resource.Configuration);
        return builder;
    }

    /// <summary>
    /// Configures the logging level for the LiveKit SIP.
    /// </summary>
    /// <param name="builder">The SIP resource builder.</param>
    /// <param name="logLevel">The log level (debug, info, warn, error).</param>
    /// <returns>The resource builder for method chaining.</returns>
    public static IResourceBuilder<LivekitSipResource> WithLogLevel(
        this IResourceBuilder<LivekitSipResource> builder,
        string logLevel = "info")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logLevel);
        builder.Resource.Configuration.Logging ??= new LoggingConfiguration();
        builder.Resource.Configuration.Logging.Level = logLevel.ToLowerInvariant();
        return builder;
    }

    /// <summary>
    /// Parses a Redis connection string into its components.
    /// Supports formats: "host:port", "host:port,password=xxx,ssl=true"
    /// </summary>
    private static (string? password, bool? sslEnabled) ParseRedisConnectionString(string connectionString)
    {
        var splitString = connectionString.Split(',');
        var password = splitString.FirstOrDefault(s => s.StartsWith("password=", StringComparison.OrdinalIgnoreCase))?
            .Substring("password=".Length);

        var sslValue = splitString.FirstOrDefault(s => s.StartsWith("ssl=", StringComparison.OrdinalIgnoreCase))?
            .Substring("ssl=".Length);

        bool? sslEnabled = null;
        if (sslValue != null && bool.TryParse(sslValue, out var ssl))
        {
            sslEnabled = ssl;
        }

        return (password, sslEnabled);
    }
}
