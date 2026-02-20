using System.Net.Http.Headers;
using Aspire.Hosting;
using Microsoft.Extensions.Logging;

namespace Livekit.Server.Sdk.Dotnet.Test;

public static class TestConstants
{
    public const string ROOM_NAME = "test-room";
    public const string ROOM_METADATA = "room-metadata";
    public const string PARTICIPANT_IDENTITY = "test-participant";
}

/// <summary>
/// Fixture that uses Aspire's DistributedApplicationTestingBuilder to start the LiveKit infrastructure.
/// This replaces the manual Testcontainers setup with Aspire's managed container orchestration.
/// </summary>
public class ServiceClientFixture : IAsyncLifetime
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(120);

    private DistributedApplication? _app;

    public string LivekitUrl { get; private set; } = default!;

    // Using the known default dev credentials from LivekitDefaults (set by WithDevMode in AppHost)
    public string ApiKey { get; private set; } = "devkey";
    public string ApiSecret { get; private set; } = "secretsecretsecretsecretsecretsecretsecret";

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Livekit_Example_AppHost>(
            args: [],
            configureBuilder: (options, settings) =>
            {
                settings.EnvironmentName = "Development";
            });

        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        _app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await _app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Wait for LiveKit server to be healthy
        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("livekit-server", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        LivekitUrl = _app.GetEndpoint("livekit-server")?.ToString() ?? throw new InvalidOperationException("LiveKit URL not found");
    }

    public async ValueTask DisposeAsync()
    {
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }

    /// <summary>
    /// Joins a participant to a room using the LiveKit CLI.
    /// </summary>
    public async Task JoinParticipant(string roomName, string participantIdentity)
    {
        var args = new[]
        {
            "room", "join",
            "--identity", participantIdentity,
            roomName
        };

        await RunLivekitCliAsync(args, waitForMessage: $"connected to room.*{roomName}");
    }

    /// <summary>
    /// Publishes a video track in a room using the LiveKit CLI.
    /// </summary>
    public async Task PublishVideoTrackInRoom(
        RoomServiceClient client,
        string roomName,
        string participantIdentity)
    {
        var args = new[]
        {
            "room", "join",
            "--identity", participantIdentity,
            "--publish-demo",
            roomName
        };

        await RunLivekitCliAsync(args, waitForMessage: "published simulcast track");

        // Wait for participant to have tracks (up to 10 seconds)
        ParticipantInfo? participant = null;
        var timeout = DateTime.Now.AddSeconds(10);
        while ((participant == null || participant.Tracks.Count == 0) && DateTime.Now < timeout)
        {
            participant = await client.GetParticipant(
                new RoomParticipantIdentity { Room = roomName, Identity = participantIdentity }
            );
            if (participant.Tracks.Count == 0)
            {
                await Task.Delay(500);
            }
        }
        Assert.NotNull(participant);
        if (participant.Tracks.Count == 0)
        {
            Assert.Fail("Participant has no tracks");
        }
    }

    private async Task RunLivekitCliAsync(string[] args, string? waitForMessage = null)
    {
        // Fall back to Docker - find the Aspire network and connect to it
        // The LiveKit server container name follows the pattern: {project}-livekit-server-{hash}
        var networkName = await GetAspireNetworkNameAsync();

        var dockerWebsocketUrl = LivekitUrl
            .Replace("http", "ws")
            .Replace("https", "wss");
            // .Replace("localhost", "livekit-server")  // Use container name as hostname
            // .Replace("127.0.0.1", "livekit-server");

        var dockerArgs = args.Concat(
        [
            "--api-key", ApiKey,
            "--api-secret", ApiSecret,
            "--url", dockerWebsocketUrl
        ]).ToArray();

        var fileName = "docker";
        var arguments = $"run --rm --network {networkName} livekit/livekit-cli:latest {string.Join(" ", dockerArgs)}";

        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();
        var messageFound = new TaskCompletionSource<bool>();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                if (waitForMessage != null && System.Text.RegularExpressions.Regex.IsMatch(e.Data, waitForMessage, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    messageFound.TrySetResult(true);
                }
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
                if (waitForMessage != null && System.Text.RegularExpressions.Regex.IsMatch(e.Data, waitForMessage, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    messageFound.TrySetResult(true);
                }
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (waitForMessage != null)
        {
            // Wait for the expected message or timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
            var completedTask = await Task.WhenAny(messageFound.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                try { process.Kill(); } catch { }
                throw new TimeoutException($"Timeout waiting for message: {waitForMessage}. Output: {outputBuilder}. Error: {errorBuilder}");
            }

            // Give it a moment to stabilize, then we're done - the CLI might still be running
            await Task.Delay(500);
        }
        else
        {
            // Wait for the process to complete
            var completed = await Task.Run(() => process.WaitForExit(30000));
            if (!completed)
            {
                try { process.Kill(); } catch { }
                throw new TimeoutException($"Timeout waiting for CLI command. Output: {outputBuilder}. Error: {errorBuilder}");
            }
        }
    }

    private async Task<string> GetAspireNetworkNameAsync()
    {
        // Find Docker networks that contain "aspire" or the livekit-server container
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "network ls --format {{.Name}}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        var networks = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Look for Aspire-created networks (typically named with project name or "aspire")
        var aspireNetwork = networks.FirstOrDefault(n =>
            n.Contains("aspire", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("livekit", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("apphost", StringComparison.OrdinalIgnoreCase));

        if (aspireNetwork != null)
        {
            return aspireNetwork;
        }

        // Fallback: find which network the livekit-server container is on
        var inspectProcess = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps --filter name=livekit-server --format {{.Names}}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        inspectProcess.Start();
        var containerName = (await inspectProcess.StandardOutput.ReadToEndAsync()).Trim();
        await inspectProcess.WaitForExitAsync();

        if (!string.IsNullOrEmpty(containerName))
        {
            var networkInspect = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"inspect {containerName} --format {{{{range $k, $v := .NetworkSettings.Networks}}}}{{{{$k}}}}{{{{end}}}}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            networkInspect.Start();
            var network = (await networkInspect.StandardOutput.ReadToEndAsync()).Trim();
            await networkInspect.WaitForExitAsync();

            if (!string.IsNullOrEmpty(network))
            {
                return network;
            }
        }

        // Last resort: use bridge network
        return "bridge";
    }
}

public class TestHttpMessageHandler : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public HttpResponseMessage? LastResponse { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        LastRequest = request;

        // Return an empty but valid protobuf message for any service client.
        var emptyContent = new ByteArrayContent(new byte[0]);
        emptyContent.Headers.ContentType = new MediaTypeHeaderValue("application/protobuf");

        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = emptyContent,
        };

        if (request.Headers.Contains("X-Test-Random-Out"))
            response.Headers.Add(
                "X-Test-Random-In",
                request.Headers.GetValues("X-Test-Random-Out")
            );

        response.Headers.Add("X-Test-Handler", "CustomHttpClientUsed");
        LastResponse = response;

        return Task.FromResult(response);
    }
}

[CollectionDefinition("Integration tests")]
public class IntegrationTestsCollection : ICollectionFixture<ServiceClientFixture>
{
    // This class has no code, and is never created.
    // Its purpose is to be the place to apply [CollectionDefinition]
    // and all the ICollectionFixture<> interfaces.
}
