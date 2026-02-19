namespace LivekitAspire;

/// <summary>
/// LiveKit CLI configuration.
/// Configuration for the LiveKit CLI utility container.
/// </summary>
public class LivekitCliConfiguration
{
    /// <summary>
    /// The project name to use for CLI commands.
    /// </summary>
    public string? Project { get; set; }

    /// <summary>
    /// API key for authentication with LiveKit server.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// API secret for authentication with LiveKit server.
    /// </summary>
    public string? ApiSecret { get; set; }

    /// <summary>
    /// URL of the LiveKit server.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Enable insecure connections (skip TLS verification).
    /// </summary>
    public bool? Insecure { get; set; }

    /// <summary>
    /// Enable verbose logging.
    /// </summary>
    public bool? Verbose { get; set; }
}
