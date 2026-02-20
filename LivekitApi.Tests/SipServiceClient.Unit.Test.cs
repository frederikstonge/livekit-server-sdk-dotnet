namespace Livekit.Server.Sdk.Dotnet.Test;

/// <summary>
/// Unit tests for SipServiceClient that don't require the Aspire infrastructure.
/// </summary>
public class SipServiceClientUnitTest
{
    [Fact]
    public async Task Constructor_UsesCustomHttpClient_HeaderIsSent()
    {
        var handler = new TestHttpMessageHandler();
        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add("X-Test-Header", "test-value");
        var randomHeaderValue = "random-value-" + Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("X-Test-Random-Out", randomHeaderValue);

        var service = new SipServiceClient(
            "http://localhost",
            "key",
            "secretsecretsecretsecretsecretsecret",
            client
        );

        await service.ListSIPInboundTrunk(new ListSIPInboundTrunkRequest());

        // The custom header is present in the outgoing request
        Assert.NotNull(handler.LastRequest);
        Assert.True(handler.LastRequest.Headers.Contains("X-Test-Header"));
        Assert.Equal(
            "test-value",
            handler.LastRequest.Headers.GetValues("X-Test-Header").First()
        );

        // The handler's response contains the marker header and echo header
        Assert.NotNull(handler.LastResponse);
        Assert.True(handler.LastResponse.Headers.Contains("X-Test-Handler"));
        Assert.Equal(
            "CustomHttpClientUsed",
            handler.LastResponse.Headers.GetValues("X-Test-Handler").First()
        );
        Assert.True(handler.LastResponse.Headers.Contains("X-Test-Random-In"));
        Assert.Equal(
            randomHeaderValue,
            handler.LastResponse.Headers.GetValues("X-Test-Random-In").First()
        );
    }
}
