namespace VehicleVision.SimComAt;

public enum SimComFamily
{
    Generic,
    Sim800,
    Sim7000,
    Sim7020,
    Sim7070,
    Sim7100,
    Sim7500_7600,
    A76xx,
    Sim76xx,
    Sim73xx,
    Sim82xx_83xx
}

[Flags]
public enum ModemCapability
{
    None = 0,
    Gsm = 1 << 0,
    Umts = 1 << 1,
    Lte = 1 << 2,
    LteM = 1 << 3,
    NbIot = 1 << 4,
    FiveG = 1 << 5,
    Sms = 1 << 6,
    PacketData = 1 << 7,
    Gnss = 1 << 8,
    Http = 1 << 9,
    Mqtt = 1 << 10,
    TcpIp = 1 << 11,
    FileSystem = 1 << 12,
    Voice = 1 << 13
}

public enum AtCommandRisk
{
    ReadOnly,
    Configuration,
    Connectivity,
    Destructive
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
    string Description = "",
    ModemCapability RequiredCapability = ModemCapability.None,
    AtCommandRisk Risk = AtCommandRisk.ReadOnly)
{
    public bool IsParameterized => Command.Contains('{');
}

public sealed record AtCommandResult(
    string Command,
    IReadOnlyList<string> Lines,
    bool IsSuccess,
    bool TimedOut,
    TimeSpan Elapsed)
{
    public string RawResponse => string.Join(Environment.NewLine, Lines);
    public IReadOnlyList<string> DataLines => Lines.Where(x => x is not "OK" and not "CONNECT").ToArray();
}

public sealed record ModemProfile(
    string Id,
    string DisplayName,
    SimComFamily Family,
    int DefaultBaudRate,
    ModemCapability Capabilities,
    IReadOnlyList<string> ModelPrefixes,
    IReadOnlyList<AtCommandDefinition> Commands)
{
    public bool Supports(ModemCapability capability) => (Capabilities & capability) == capability;
}

public sealed record ModemIdentity(
    string Manufacturer,
    string Model,
    string Revision,
    ModemProfile Profile);

public sealed record AtTraceEntry(DateTimeOffset Timestamp, bool IsTransmit, string Text);

public enum PdpType
{
    IP,
    IPV6,
    IPV4V6
}

public enum PdpAuthentication
{
    None = 0,
    Pap = 1,
    Chap = 2,
    PapOrChap = 3
}

public sealed record PdpContextSettings(
    int ContextId,
    string Apn,
    PdpType PdpType = PdpType.IP,
    PdpAuthentication Authentication = PdpAuthentication.None,
    string UserName = "",
    string Password = "");

public sealed record AtWorkflowResult(IReadOnlyList<AtCommandResult> Steps)
{
    public bool IsSuccess => Steps.Count > 0 && Steps.All(x => x.IsSuccess);
    public AtCommandResult? FailedStep => Steps.FirstOrDefault(x => !x.IsSuccess);
}
