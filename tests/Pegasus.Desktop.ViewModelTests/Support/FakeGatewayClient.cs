using Pegasus.Contracts.ProblemDetails;

namespace Pegasus.Desktop.ViewModelTests.Support;

public sealed record RecordedGatewayRequest(
    string Operation,
    HttpMethod Method,
    Uri? Uri,
    object? Body);

public sealed record FakeGatewayResponse(object? Value, PegasusProblem? Problem)
{
    public bool Succeeded => Problem is null;
}

/// <summary>
/// A transport-free gateway seam for view-model tests. The real generated
/// client is supplied by the gateway tickets; this test seam deliberately
/// records calls and returns queued values without opening a socket.
/// </summary>
public sealed class FakeGatewayClient
{
    private readonly Queue<FakeGatewayResponse> _responses = new();

    public List<RecordedGatewayRequest> Requests { get; } = [];

    public void EnqueueResponse(object? value) =>
        _responses.Enqueue(new FakeGatewayResponse(value, null));

    public void EnqueueProblem(
        string type,
        int status = 400,
        string title = "Test problem",
        string? detail = null)
    {
        _responses.Enqueue(new FakeGatewayResponse(
            null,
            new PegasusProblem(type, title, status, detail, null, "test-correlation")));
    }

    public Task<FakeGatewayResponse> SendAsync(
        string operation,
        HttpMethod? method = null,
        Uri? uri = null,
        object? body = null)
    {
        Requests.Add(new RecordedGatewayRequest(
            operation,
            method ?? HttpMethod.Get,
            uri,
            body));

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("The fake gateway response queue is empty.");
        }

        return Task.FromResult(_responses.Dequeue());
    }
}
