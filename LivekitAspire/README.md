# LiveKit .NET Aspire Integration

A comprehensive .NET Aspire hosting integration for [LiveKit](https://livekit.io/), the open-source platform for voice, video, and AI agents.

## Overview

This package provides .NET Aspire resource integration for the complete LiveKit ecosystem:

| Service | Description | Resource Type |
|---------|-------------|---------------|
| **LiveKit Server** | Core WebRTC SFU for realtime communication | `LivekitServerResource` |
| **Egress** | Recording and streaming output | `LivekitEgressResource` |
| **Ingress** | Streaming input (RTMP, WHIP, URL pull) | `LivekitIngressResource` |
| **SIP** | Telephony integration (SIP/PSTN) | `LivekitSipResource` |
| **CLI** | Command-line utilities (room management, tokens, load testing) | `LivekitCliResource` |

## Quick Start

### Basic Setup

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add LiveKit server with development defaults
var livekit = builder.AddLivekitServer("livekit");

// Add your API project with LiveKit reference
builder.AddProject<Projects.MyApi>("api")
    .WithReference(livekit);  // Injects all required environment variables

builder.Build().Run();
```

### Full Production Setup

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Redis for clustering (required for production)
var redis = builder.AddRedis("redis");

// Add LiveKit server with Redis and configuration
var livekit = builder.AddLivekitServer("livekit", httpPort: 7880)
    .WithRedis(redis)
    .WithDevMode()
    .WithLogLevel("debug");

// Add supporting services
var egress = builder.AddLivekitEgress("egress", livekit, redis);
var ingress = builder.AddLivekitIngress("ingress", livekit, redis);
var sip = builder.AddLivekitSip("sip", livekit, redis);

// Add your API with LiveKit integration
builder.AddProject<Projects.MyApi>("api")
    .WithReference(livekit);

builder.Build().Run();
```

## Environment Variables

When you reference a LiveKit server in your project, the following environment variables are automatically injected:

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__livekit` | WebSocket URL for client connections | `ws://localhost:7880` |
| `LIVEKIT_URL` | WebSocket URL (same as connection string) | `ws://localhost:7880` |
| `LIVEKIT_API_KEY` | API key for authentication | `devkey` |
| `LIVEKIT_API_SECRET` | API secret for authentication | `secret...` |

## Service Configuration

### LiveKit Server

```csharp
builder.AddLivekitServer("livekit",
    httpPort: 7880,           // HTTP/WebSocket port (default: auto-assigned)
    rtcTcpPort: 7881,         // RTC TCP port for media
    apiKey: myApiKeyParam,    // Custom API key parameter
    apiSecret: mySecretParam, // Custom API secret parameter
    imageTag: "latest")       // Container image tag
    .WithRedis(redis)         // Redis for clustering
    .WithDevMode()            // Enable development mode
    .WithLogLevel("debug")    // Set log level (debug, info, warn, error)
    .WithTurn();              // Enable built-in TURN server
```

### Egress Service

Egress handles recording sessions and streaming output to external destinations (HLS, RTMP, S3, etc.).

```csharp
builder.AddLivekitEgress("egress", livekit, redis,
    imageTag: "latest")
    .WithFileOutput("/recordings"); // Local file output directory
```

### Ingress Service

Ingress enables streaming input via RTMP, WHIP, or URL pull.

```csharp
builder.AddLivekitIngress("ingress", livekit, redis,
    rtmpPort: 1935,    // RTMP streaming port
    whipPort: 8085,    // WHIP WebRTC streaming port
    imageTag: "latest");
```

### SIP Service

SIP service enables telephony integration with SIP/PSTN networks.

```csharp
builder.AddLivekitSip("sip", livekit, redis,
    sipPort: 5060,     // SIP signaling port
    imageTag: "latest");
```

### CLI

The LiveKit CLI provides utilities for interacting with LiveKit servers, including room management, token generation, load testing, and administrative tasks.

```csharp
builder.AddLivekitCli("cli", livekit,
    imageTag: "latest")
    .WithVerbose();  // Enable verbose logging
```

Example CLI commands:
- `lk room list` - List all rooms
- `lk room create <room-name>` - Create a room
- `lk token create --room <room> --identity <identity>` - Generate a token
- `lk load-test` - Run load tests

## Using Custom API Credentials

For production, use Aspire parameters to manage credentials securely:

```csharp
// Define secure parameters
var apiKey = builder.AddParameter("livekit-api-key");
var apiSecret = builder.AddParameter("livekit-api-secret", secret: true);

// Use with LiveKit server
var livekit = builder.AddLivekitServer("livekit",
    apiKey: apiKey,
    apiSecret: apiSecret);
```

Configure parameters via:
- **Environment variables**: `PARAMETERS__LIVEKIT_API_KEY=your-key`
- **User secrets**: `dotnet user-secrets set "Parameters:livekit-api-secret" "your-secret"`
- **appsettings.json**: `"Parameters": { "livekit-api-key": "..." }`

## Application Integration

### ASP.NET Core Service

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Configuration is automatically available from environment variables
var livekitUrl = builder.Configuration["LIVEKIT_URL"];
var apiKey = builder.Configuration["LIVEKIT_API_KEY"];
var apiSecret = builder.Configuration["LIVEKIT_API_SECRET"];

// Or use the connection string
var wsUrl = builder.Configuration.GetConnectionString("livekit");

// Register LiveKit services
builder.Services.AddSingleton<RoomServiceClient>(sp =>
{
    var uri = new Uri(wsUrl!);
    return new RoomServiceClient($"http://{uri.Host}:{uri.Port}", apiKey!, apiSecret!);
});
```

### Token Generation

```csharp
public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public string GenerateToken(string roomName, string participantIdentity)
    {
        var apiKey = _config["LIVEKIT_API_KEY"]!;
        var apiSecret = _config["LIVEKIT_API_SECRET"]!;

        return new AccessToken(apiKey, apiSecret)
            .WithIdentity(participantIdentity)
            .WithGrants(new VideoGrants
            {
                Room = roomName,
                CanPublish = true,
                CanSubscribe = true
            })
            .ToJwt();
    }
}
```

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         Aspire AppHost                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────┐     ┌─────────────────────────────────────────┐  │
│   │  Redis  │◄────│              LiveKit Server             │  │
│   └────┬────┘     │  (WebRTC SFU, Room Management, Webhooks)│  │
│        │          └────────────────┬────────────────────────┘  │
│        │                           │                            │
│        ▼                           ▼                            │
│   ┌─────────┐     ┌──────────┐ ┌──────────┐ ┌──────────┐       │
│   │Your API │◄────│  Egress  │ │ Ingress  │ │   SIP    │       │
│   │(tokens, │     │(recording│ │(RTMP/WHIP│ │(telephony│       │
│   │ rooms)  │     │streaming)│ │ input)   │ │  bridge) │       │
│   └─────────┘     └──────────┘ └──────────┘ └──────────┘       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Port Reference

| Service | Port | Protocol | Purpose |
|---------|------|----------|---------|
| LiveKit Server | 7880 | HTTP/WS | API and WebSocket signaling |
| LiveKit Server | 7881 | TCP | RTC media transport |
| LiveKit Server | 3478 | UDP | TURN server (optional) |
| Egress | 9091 | HTTP | Health check |
| Ingress | 1935 | TCP | RTMP streaming |
| Ingress | 8085 | HTTP | WHIP WebRTC streaming |
| Ingress | 9090 | HTTP | HTTP relay |
| Ingress | 9091 | HTTP | Health check |
| SIP | 5060 | UDP | SIP signaling |
| SIP | 10000-20000 | UDP | RTP media |

## Health Checks

All LiveKit services include automatic health checks:

- **LiveKit Server**: HTTP health check on `/`
- **Egress**: HTTP health check on `/`
- **Ingress**: HTTP health check on `/`
- **SIP**: Log-based health check (waits for "sip signaling listening on")

## Development vs Production

### Development Mode

```csharp
var livekit = builder.AddLivekitServer("livekit")
    .WithDevMode()       // Simplified configuration for local dev
    .WithLogLevel("debug");
```

### Production Mode

```csharp
var redis = builder.AddRedis("redis");  // Required for clustering

var apiKey = builder.AddParameter("livekit-api-key");
var apiSecret = builder.AddParameter("livekit-api-secret", secret: true);

var livekit = builder.AddLivekitServer("livekit",
    apiKey: apiKey,
    apiSecret: apiSecret)
    .WithRedis(redis)
    .WithLogLevel("info")
    .WithTurn();  // Enable TURN for NAT traversal
```

## Troubleshooting

### Common Issues

1. **Connection refused**: Ensure the LiveKit server container is running and healthy
2. **Authentication failed**: Check that API key and secret match between server and client
3. **Redis connection issues**: Verify Redis is available and the address is correct
4. **Port conflicts**: Ensure configured ports are available on the host

### Debugging

Enable debug logging to see detailed connection information:

```csharp
var livekit = builder.AddLivekitServer("livekit")
    .WithLogLevel("debug");
```

## See Also

- [LiveKit Documentation](https://docs.livekit.io/)
- [LiveKit .NET Server SDK](https://github.com/livekit/server-sdk-dotnet)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [LiveKit Self-Hosting Guide](https://docs.livekit.io/deploy/self-hosting/)
