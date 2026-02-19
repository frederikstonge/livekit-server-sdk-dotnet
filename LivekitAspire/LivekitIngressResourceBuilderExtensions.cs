using Aspire.Hosting.ApplicationModel;
using LivekitAspire;
using Microsoft.Extensions.Hosting;

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
    /// <param name="httpRelayPort">The HTTP relay port. Defaults to 9090.</param>
    /// <param name="healthPort">The health check port. Defaults to 9091.</param>
    /// <param name="imageTag">The container image tag. Defaults to "latest".</param>
    /// <returns>An <see cref="IResourceBuilder{LivekitIngressResource}"/> for the ingress service.</returns>
    public static IResourceBuilder<LivekitIngressResource> AddLivekitIngress(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<LivekitServerResource> livekitServer,
        IResourceBuilder<IResource>? redis = null,
        int rtmpPort = 1935,
        int whipPort = 8085,
        int httpRelayPort = 9090,
        int healthPort = 9091,
        string imageTag = "latest")
    {
        ArgumentNullException.ThrowIfNull(livekitServer);

        var resource = new LivekitIngressResource(name)
        {
            ServerResource = livekitServer,
            RedisResource = redis
        };

        var configDir = Path.Combine(Path.GetTempPath(), "livekit-config", name);

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
            
            // Configure ports
            resource.Configuration.RtmpPort = rtmpPort;
            resource.Configuration.WhipPort = whipPort;
            resource.Configuration.HttpRelayPort = httpRelayPort;
            resource.Configuration.HealthPort = healthPort;

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

            // Write the YAML configuration file
            Directory.CreateDirectory(configDir);
            var yaml = resource.Configuration.ToYaml();
            File.WriteAllText(Path.Combine(configDir, "config.yaml"), yaml);
        });

        var ingressBuilder = builder.AddResource(resource)
            .WaitFor(livekitServer)
            .WithImage(LivekitContainerImageTags.IngressImage)
            .WithImageRegistry(LivekitContainerImageTags.Registry)
            .WithImageTag(imageTag)
            .WithHttpEndpoint(targetPort: healthPort, port: healthPort, name: LivekitIngressResource.HttpEndpointName)
            .WithEndpoint(targetPort: rtmpPort, port: rtmpPort, name: LivekitIngressResource.RtmpEndpointName, scheme: "tcp")
            .WithEndpoint(targetPort: whipPort, port: whipPort, name: LivekitIngressResource.WhipEndpointName)
            .WithEndpoint(targetPort: httpRelayPort, port: httpRelayPort, name: LivekitIngressResource.HttpRelayEndpointName)
            .WithBindMount(configDir, "/config", isReadOnly: true)
            .WithEnvironment("INGRESS_CONFIG_FILE", "/config/config.yaml")
            .WithHttpHealthCheck("/");
            

        if (redis != null)
        {
            ingressBuilder = ingressBuilder.WaitFor(redis);
        }

        return ingressBuilder;
    }

    /// <summary>
    /// Configures the LiveKit ingress using a configuration object that will be serialized to YAML.
    /// </summary>
    /// <param name="builder">The ingress resource builder.</param>
    /// <param name="configure">An action to configure the LiveKit ingress settings.</param>
    /// <returns>The resource builder for method chaining.</returns>
    public static IResourceBuilder<LivekitIngressResource> WithConfiguration(
        this IResourceBuilder<LivekitIngressResource> builder,
        Action<LivekitIngressConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(builder.Resource.Configuration);
        return builder;
    }

    /// <summary>
    /// Configures the logging level for the LiveKit ingress.
    /// </summary>
    /// <param name="builder">The ingress resource builder.</param>
    /// <param name="logLevel">The log level (debug, info, warn, error).</param>
    /// <returns>The resource builder for method chaining.</returns>
    public static IResourceBuilder<LivekitIngressResource> WithLogLevel(
        this IResourceBuilder<LivekitIngressResource> builder,
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
