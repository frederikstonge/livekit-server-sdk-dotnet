namespace Livekit.Example.AppHost;

public class AppHost
{
    public static async Task Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // Add Redis for LiveKit clustering/scaling
        var redis = builder.AddRedis("redis");

        // Add LiveKit server with Redis integration and development mode
        var livekit = builder.AddLivekitServer("livekit-server", httpPort: 7880)
            .WithRedis(redis)
            .WithDevMode()
            .WithLogLevel("debug")
            .WithConfiguration(config =>
            {
                // Configure RTC settings
                config.Rtc = new LivekitAspire.RtcConfiguration
                {
                    PortRangeStart = 50000,
                    PortRangeEnd = 60000,
                    UseExternalIp = false
                };

                // Configure default room settings
                config.Room = new LivekitAspire.RoomConfiguration
                {
                    EmptyTimeout = 300,
                    DepartureTimeout = 20
                };
            });

        // Add Egress service for recording/streaming output
        var egress = builder.AddLivekitEgress("livekit-egress", livekit, redis)
            .WithLogLevel("debug")
            .WithConfiguration(config =>
            {
            });

        // Add Ingress service for streaming input (RTMP/WHIP)
        var ingress = builder.AddLivekitIngress("livekit-ingress", livekit, redis)
            .WithLogLevel("debug")
            .WithConfiguration(config =>
            {
            });

        // Add SIP service for telephony integration
        var sip = builder.AddLivekitSip("livekit-sip", livekit, redis)
            .WithLogLevel("debug")
            .WithConfiguration(config =>
            {
            });

        // Add CLI for administrative tasks
        var cli = builder.RunLivekitCliCommand("cli", livekit, ["--help"])
            .WithVerbose()
            .WithConfiguration(config =>
            {
            })
            .WithExplicitStart();

        // Add API service with LiveKit reference
        // This automatically injects:
        // - ConnectionStrings__livekit-server (WebSocket URL)
        // - LIVEKIT_API_KEY
        // - LIVEKIT_API_SECRET  
        // - LIVEKIT_URL
        var apiService = builder.AddProject<Projects.Livekit_Example_Api>("apiservice")
            .WithReference(livekit)
            .WaitFor(livekit)
            .WithHttpHealthCheck("/health");
        
        await builder.Build().RunAsync();
    }
}
