namespace VehicleVision.SimComAt;

public sealed class GnssService(AtCommandClient client, ModemProfile profile)
{
    public Task<AtCommandResult> PowerOnAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(x => x.PowerOn, cancellationToken);

    public Task<AtCommandResult> PowerOffAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(x => x.PowerOff, cancellationToken);

    public Task<AtCommandResult> GetInformationAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(x => x.Information, cancellationToken);

    public static bool TryGetCommands(SimComFamily family, out GnssCommandSet commands)
    {
        commands = family switch
        {
            SimComFamily.Sim800 or SimComFamily.Sim7000 or SimComFamily.Sim7070 =>
                new("AT+CGNSPWR=1", "AT+CGNSPWR=0", "AT+CGNSINF"),
            SimComFamily.A76xx or SimComFamily.Sim76xx =>
                new("AT+CGNSSPWR=1", "AT+CGNSSPWR=0", "AT+CGNSSINFO"),
            SimComFamily.Sim7100 or SimComFamily.Sim7500_7600 or SimComFamily.Sim73xx or SimComFamily.Sim82xx_83xx =>
                new("AT+CGPS=1", "AT+CGPS=0", "AT+CGPSINFO"),
            _ => null!
        };
        return commands is not null;
    }

    private Task<AtCommandResult> ExecuteAsync(Func<GnssCommandSet, string> selector, CancellationToken cancellationToken)
    {
        if (!profile.Supports(ModemCapability.Gnss) || !TryGetCommands(profile.Family, out var commands))
            throw new NotSupportedException($"{profile.DisplayName}のGNSSコマンドは登録されていません。");
        return client.ExecuteAsync(selector(commands), TimeSpan.FromSeconds(30), cancellationToken);
    }
}
