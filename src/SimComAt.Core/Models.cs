namespace VehicleVision.SimComAt;

public enum SimComModel
{
    SIM7100Jx,
    SIM7600JC_H,
    SIM7312G_M2,
    SIM8262E_M2
}

public sealed record SerialConnectionOptions(
    string PortName,
    int BaudRate = 115200,
    int DataBits = 8,
    System.IO.Ports.Parity Parity = System.IO.Ports.Parity.None,
    System.IO.Ports.StopBits StopBits = System.IO.Ports.StopBits.One,
    System.IO.Ports.Handshake Handshake = System.IO.Ports.Handshake.None);

public sealed record AtCommandDefinition(
    string Key,
    string DisplayName,
    string Command,
    string Category,
    string Description = "");

public sealed record AtCommandResult(
    string Command,
    IReadOnlyList<string> Lines,
    bool IsSuccess,
    bool TimedOut,
    TimeSpan Elapsed)
{
    public string RawResponse => string.Join(Environment.NewLine, Lines);
}

public sealed record ModemProfile(
    SimComModel Model,
    string DisplayName,
    int DefaultBaudRate,
    IReadOnlyList<AtCommandDefinition> Commands);

public sealed record AtTraceEntry(DateTimeOffset Timestamp, bool IsTransmit, string Text);
