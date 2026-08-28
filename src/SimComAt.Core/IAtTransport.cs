namespace VehicleVision.SimComAt;

public interface IAtTransport : IAsyncDisposable
{
    bool IsOpen { get; }
    event EventHandler<AtTraceEntry>? Trace;
    Task OpenAsync(CancellationToken cancellationToken = default);
    Task CloseAsync(CancellationToken cancellationToken = default);
    Task WriteLineAsync(string command, CancellationToken cancellationToken = default);
    Task<string?> ReadLineAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}
