namespace VehicleVision.SimComAt;

public static class ModemDetector
{
    public static async Task<ModemIdentity> DetectAsync(AtCommandClient client, CancellationToken cancellationToken = default)
    {
        await client.ExecuteAsync("ATE0", cancellationToken: cancellationToken);
        var manufacturer = await ReadValueAsync(client, "AT+CGMI", cancellationToken);
        var model = await ReadValueAsync(client, "AT+CGMM", cancellationToken);
        var revision = await ReadValueAsync(client, "AT+CGMR", cancellationToken);

        if (string.IsNullOrWhiteSpace(model))
            model = await ReadValueAsync(client, "ATI", cancellationToken);

        return new ModemIdentity(manufacturer, model, revision, ModemProfiles.Match(model));
    }

    private static async Task<string> ReadValueAsync(AtCommandClient client, string command, CancellationToken cancellationToken)
    {
        var result = await client.ExecuteAsync(command, cancellationToken: cancellationToken);
        return result.IsSuccess
            ? string.Join(" ", result.DataLines.Where(x => !x.StartsWith("AT", StringComparison.OrdinalIgnoreCase))).Trim()
            : string.Empty;
    }
}
