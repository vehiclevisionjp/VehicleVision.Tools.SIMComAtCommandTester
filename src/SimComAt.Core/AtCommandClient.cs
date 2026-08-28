using System.Diagnostics;

namespace VehicleVision.SimComAt;

public sealed class AtCommandClient(IAtTransport transport)
{
    private readonly SemaphoreSlim _commandLock = new(1, 1);

    public bool IsConnected => transport.IsOpen;
    public event EventHandler<AtTraceEntry>? Trace
    {
        add => transport.Trace += value;
        remove => transport.Trace -= value;
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default) => transport.OpenAsync(cancellationToken);
    public Task DisconnectAsync(CancellationToken cancellationToken = default) => transport.CloseAsync(cancellationToken);

    public async Task<AtCommandResult> ExecuteAsync(
        string command,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        command = Normalize(command);
        var limit = timeout ?? TimeSpan.FromSeconds(10);
        await _commandLock.WaitAsync(cancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var lines = new List<string>();
            await transport.WriteLineAsync(command, cancellationToken);

            while (stopwatch.Elapsed < limit)
            {
                var remaining = limit - stopwatch.Elapsed;
                var line = await transport.ReadLineAsync(remaining, cancellationToken);
                if (line is null)
                    return new(AtCommandRedactor.Redact(command), lines, false, true, stopwatch.Elapsed);

                if (string.Equals(line, command, StringComparison.OrdinalIgnoreCase)) continue;
                lines.Add(AtCommandRedactor.Redact(line));
                if (IsSuccess(line)) return new(AtCommandRedactor.Redact(command), lines, true, false, stopwatch.Elapsed);
                if (IsError(line)) return new(AtCommandRedactor.Redact(command), lines, false, false, stopwatch.Elapsed);
            }
            return new(AtCommandRedactor.Redact(command), lines, false, true, stopwatch.Elapsed);
        }
        finally
        {
            _commandLock.Release();
        }
    }

    private static string Normalize(string command)
    {
        var value = command.Trim().TrimEnd('\r', '\n');
        if (value.Length == 0) throw new ArgumentException("ATコマンドを入力してください。", nameof(command));
        if (!value.StartsWith("AT", StringComparison.OrdinalIgnoreCase)) value = "AT" + value;
        return value;
    }

    private static bool IsSuccess(string line) => line is "OK" or "CONNECT" || line.StartsWith("CONNECT ", StringComparison.Ordinal);
    private static bool IsError(string line) =>
        line is "ERROR" or "NO CARRIER" or "NO ANSWER" or "BUSY" ||
        line.StartsWith("+CME ERROR", StringComparison.Ordinal) ||
        line.StartsWith("+CMS ERROR", StringComparison.Ordinal);
}
