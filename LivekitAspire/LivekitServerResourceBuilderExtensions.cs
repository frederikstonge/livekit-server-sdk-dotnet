using Aspire.Hosting.ApplicationModel;
using LivekitAspire;
using Microsoft.Extensions.Hosting;

// Put extensions in the Aspire.Hosting namespace to ease discovery as referencing
// the Aspire hosting package automatically adds this namespace.
namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding LiveKit resources to the distributed application builder.
/// </summary>
public static class LivekitServerResourceBuilderExtensions
{
    /// <summary>
    /// Adds a LiveKit server container resource to the application.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="httpPort">The HTTP/WebSocket port. Defaults to 7880 inside the container.</param>
    /// <param name="rtcTcpPort">The RTC TCP port for media transport. Defaults to 7881 inside the container.</param>
    /// <param name="imageTag">The container image tag. Defaults to "latest".</param>
    /// <returns>An <see cref="IResourceBuilder{LivekitServerResource}"/> for the LiveKit server.</returns>
    /// <remarks>
    /// Use <see cref="WithDevMode"/> for development without authentication, or
    /// <see cref="WithApiCredentials"/> to configure API key/secret for production.
    /// </remarks>
    public static IResourceBuilder<LivekitServerResource> AddLivekitServer(
        this IDistributedApplicationBuilder builder,
        string name,
        int? httpPort = null,
        int? rtcTcpPort = null,
        string imageTag = "latest")
    {
        var resource = new LivekitServerResource(name);

        var configDir = Path.Combine(Path.GetTempPath(), "livekit-config", name);

        // Register a callback to resolve deferred configuration and write the YAML file
        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(resource, async (@event, ct) =>
        {
            // Resolve Redis configuration if a Redis resource is attached
            if (resource.RedisResource != null)
            {
                var redisResource = resource.RedisResource.Resource;

                // For container-to-container communication, we need the internal container network address
                // Use the container name and internal port (6379) instead of the host-mapped port
                if (redisResource is IResourceWithEndpoints redisWithEndpoints)
                {
                    var endpoint = redisWithEndpoints.GetEndpoint("tcp");
                    // Use the container name as the host for internal container networking
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
                            // For container-to-container communication with Aspire's Redis,
                            // the TLS certificate is valid for "localhost" but not for container names.
                            // We need to set insecure: true to skip certificate verification.
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

        return builder.AddResource(resource)
            .WithImage(LivekitContainerImageTags.ServerImage)
            .WithImageRegistry(LivekitContainerImageTags.Registry)
            .WithImageTag(imageTag)
            .WithHttpEndpoint(
                targetPort: 7880,
                port: httpPort,
                name: LivekitServerResource.HttpEndpointName)
            .WithEndpoint(
                targetPort: 7881,
                port: rtcTcpPort,
                name: LivekitServerResource.RtcTcpEndpointName,
                scheme: "tcp")
            .WithBindMount(configDir, "/etc/livekit", isReadOnly: true)
            .WithArgs("--config", "/etc/livekit/config.yaml")
            .WithHttpHealthCheck("/");
    }

    /// <summary>
    /// Configures API credentials for the LiveKit server.
    /// This is required for production use. For development, use <see cref="WithDevMode"/> instead.
    /// </summary>
    /// <param name="builder">The LiveKit server resource builder.</param>
    /// <param name="apiKey">The API key parameter for authentication.</param>
    /// <param name="apiSecret">The API secret parameter for authentication.</param>
    /// <returns>The resource builder for method chaining.</returns>
    public static IResourceBuilder<LivekitServerResource> WithApiCredentials(
        this IResourceBuilder<LivekitServerResource> builder,
        IResourceBuilder<ParameterResource> apiKey,
        IResourceBuilder<ParameterResource> apiSecret)
    {
        ArgumentNullException.ThrowIfNull(apiKey);
        ArgumentNullException.ThrowIfNull(apiSecret);

        builder.Resource.ApiKeyParameter = apiKey;
        builder.Resource.ApiSecretParameter = apiSecret;

        return builder.WithEnvironment(context =>
        {
            // API keys are set via environment variable (format: "key1: secret1")
            context.EnvironmentVariables["LIVEKIT_KEYS"] = ReferenceExpression.Create(
                $"{apiKey.Resource}: {apiSecret.Resource}"
            );
        });
    }


    /// <summary>
    /// Configures the LiveKit server to use Redis for clustering and scaling.
    /// </summary>
    /// <param name="builder">The LiveKit server resource builder.</param>
    /// <param name="redis">The Redis resource to use.</param>
    /// <returns>The resource builder for method chaining.</returns>
    public static IResourceBuilder<LivekitServerResource> WithRedis(
        this IResourceBuilder<LivekitServerResource> builder,
        IResourceBuilder<IResource> redis)
    {
        ArgumentNullException.ThrowIfNull(redis);

        builder.Resource.RedisResource = redis;
        return builder.WaitFor(redis);
    }

    /// <summary>
    /// Enables development mode for the LiveKit server.
    /// In dev mode, LiveKit allows connections without strict authentication.
    /// Default API credentials will be set for resources that reference this server.
    /// </summary>
    /// <param name="builder">The LiveKit server resource builder.</param>
    /// <param name="enabled">Whether to enable development mode. Defaults to true.</param>
    /// <returns>The resource builder for method chaining.</returns>
    public static IResourceBuilder<LivekitServerResource> WithDevMode(
        this IResourceBuilder<LivekitServerResource> builder)
    {

        // Mark dev mode as enabled so WithReference knows to use defaults
        builder.Resource.DevModeEnabled = true;

        // Set default credentials for resources that will reference this server
        if (builder.Resource.ApiKeyParameter == null)
        {
            builder.Resource.ApiKeyParameter = builder.ApplicationBuilder.AddParameter(
                $"{builder.Resource.Name}-api-key", LivekitDefaults.ApiKey);
        }
        if (builder.Resource.ApiSecretParameter == null)
        {
            builder.Resource.ApiSecretParameter = builder.ApplicationBuilder.AddParameter(
                $"{builder.Resource.Name}-api-secret", LivekitDefaults.ApiSecret, secret: true);
        }

        // Dev mode is handled via command-line argument
        builder.WithArgs("--dev");
        return builder;
    }

    /// <summary>
    /// Configures the logging level for the LiveKit server.
    /// </summary>
    /// <param name="builder">The LiveKit server resource builder.</param>
    /// <param name="logLevel">The log level (debug, info, warn, error).</param>
    /// <returns>The resource builder for method chaining.</returns>
    public static IResourceBuilder<LivekitServerResource> WithLogLevel(
        this IResourceBuilder<LivekitServerResource> builder,
        string logLevel = "info")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logLevel);
        builder.Resource.Configuration.Logging ??= new LoggingConfiguration();
        builder.Resource.Configuration.Logging.Level = logLevel.ToLowerInvariant();
        return builder;
    }

    /// <summary>
    /// Configures the LiveKit server using a configuration object that will be serialized to YAML.
    /// </summary>
    /// <param name="builder">The LiveKit server resource builder.</param>
    /// <param name="configure">An action to configure the LiveKit server settings.</param>
    /// <returns>The resource builder for method chaining.</returns>
    public static IResourceBuilder<LivekitServerResource> WithConfiguration(
        this IResourceBuilder<LivekitServerResource> builder,
        Action<LivekitServerConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(builder.Resource.Configuration);
        return builder;
    }

    /// <summary>
    /// Configures TURN server settings for the LiveKit server.
    /// </summary>
    /// <param name="builder">The LiveKit server resource builder.</param>
    /// <param name="enabled">Whether to enable the built-in TURN server.</param>
    /// <param name="udpPort">The UDP port for TURN. Defaults to 3478.</param>
    /// <returns>The resource builder for method chaining.</returns>
    public static IResourceBuilder<LivekitServerResource> WithTurn(
        this IResourceBuilder<LivekitServerResource> builder,
        bool enabled = true,
        int? udpPort = null)
    {
        if (enabled)
        {
            // Configure TURN in the YAML configuration
            builder.Resource.Configuration.Turn ??= new TurnConfiguration();
            builder.Resource.Configuration.Turn.Enabled = true;
            builder.Resource.Configuration.Turn.UdpPort = udpPort ?? 3478;

            // Expose the TURN UDP port
            builder.WithEndpoint(
                targetPort: 3478,
                port: udpPort,
                name: "turn-udp",
                scheme: "udp");
        }
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

    /// <summary>
    /// Adds a reference to a LiveKit server and configures the project with required environment variables.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <param name="livekitServer">The LiveKit server resource.</param>
    /// <returns>The resource builder for method chaining.</returns>
    public static IResourceBuilder<ProjectResource> WithReference(
        this IResourceBuilder<ProjectResource> builder,
        IResourceBuilder<LivekitServerResource> livekitServer)
    {
        var result = builder
            .WithReference(livekitServer as IResourceBuilder<IResourceWithConnectionString>)
            .WithEnvironment(context =>
            {
                // Set API credentials - these will be available if WithDevMode or WithApiCredentials was called
                if (livekitServer.Resource.ApiKeyParameter != null)
                {
                    context.EnvironmentVariables["LIVEKIT_API_KEY"] = livekitServer.Resource.ApiKeyParameter.Resource;
                }
                if (livekitServer.Resource.ApiSecretParameter != null)
                {
                    context.EnvironmentVariables["LIVEKIT_API_SECRET"] = livekitServer.Resource.ApiSecretParameter.Resource;
                }
                context.EnvironmentVariables["LIVEKIT_URL"] = livekitServer.Resource.ConnectionStringExpression;
            })
            .WaitFor(livekitServer);

        return result;
    }
}