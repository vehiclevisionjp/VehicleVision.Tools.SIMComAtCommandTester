using System.IO.Ports;

namespace VehicleVision.SimComAt;

public sealed class SerialAtTransport : IAtTransport
{
    private readonly SerialPort _port;

    public SerialAtTransport(SerialConnectionOptions options)
    {
        _port = new SerialPort(options.PortName, options.BaudRate, options.Parity, options.DataBits, options.StopBits)
        {
            Handshake = options.Handshake,
            NewLine = "\r\n",
            Encoding = System.Text.Encoding.ASCII,
            ReadTimeout = 500,
            WriteTimeout = 2000,
            DtrEnable = true,
            RtsEnable = options.Handshake == Handshake.None
        };
    }

    public bool IsOpen => _port.IsOpen;
    public event EventHandler<AtTraceEntry>? Trace;

    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_port.IsOpen) _port.Open();
        _port.DiscardInBuffer();
        _port.DiscardOutBuffer();
        return Task.CompletedTask;
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_port.IsOpen) _port.Close();
        return Task.CompletedTask;
    }

    public async Task WriteLineAsync(string command, CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        Trace?.Invoke(this, new AtTraceEntry(DateTimeOffset.Now, true, command));
        await _port.BaseStream.WriteAsync(System.Text.Encoding.ASCII.GetBytes(command + "\r"), cancellationToken);
        await _port.BaseStream.FlushAsync(cancellationToken);
    }

    public async Task<string?> ReadLineAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        var buffer = new List<byte>();
        var one = new byte[1];
        try
        {
            while (true)
            {
                var read = await _port.BaseStream.ReadAsync(one, timeoutCts.Token);
                if (read == 0) return null;
                if (one[0] == (byte)'\n')
                {
                    var line = System.Text.Encoding.ASCII.GetString(buffer.ToArray()).TrimEnd('\r');
                    if (line.Length == 0) { buffer.Clear(); continue; }
                    Trace?.Invoke(this, new AtTraceEntry(DateTimeOffset.Now, false, line));
                    return line;
                }
                buffer.Add(one[0]);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    public ValueTask DisposeAsync()
    {
        _port.Dispose();
        return ValueTask.CompletedTask;
    }

    private void EnsureOpen()
    {
        if (!_port.IsOpen) throw new InvalidOperationException("シリアルポートが開かれていません。");
    }
}
