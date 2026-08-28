namespace VehicleVision.SimComAt;

public sealed class PdpContextService(AtCommandClient client)
{
    public async Task<AtWorkflowResult> ConfigureAsync(PdpContextSettings settings, CancellationToken cancellationToken = default)
    {
        Validate(settings);
        var steps = new List<AtCommandResult>();
        var apn = Quote(settings.Apn);
        steps.Add(await client.ExecuteAsync($"AT+CGDCONT={settings.ContextId},\"{settings.PdpType}\",\"{apn}\"", cancellationToken: cancellationToken));
        if (!steps[^1].IsSuccess) return new(steps);

        var auth = (int)settings.Authentication;
        var authenticationCommand = settings.Authentication == PdpAuthentication.None
            ? $"AT+CGAUTH={settings.ContextId},0"
            : $"AT+CGAUTH={settings.ContextId},{auth},\"{Quote(settings.Password)}\",\"{Quote(settings.UserName)}\"";
        steps.Add(await client.ExecuteAsync(authenticationCommand, cancellationToken: cancellationToken));
        return new(steps);
    }

    public Task<AtCommandResult> AttachAsync(CancellationToken cancellationToken = default) =>
        client.ExecuteAsync("AT+CGATT=1", TimeSpan.FromSeconds(60), cancellationToken);

    public Task<AtCommandResult> DetachAsync(CancellationToken cancellationToken = default) =>
        client.ExecuteAsync("AT+CGATT=0", TimeSpan.FromSeconds(60), cancellationToken);

    public Task<AtCommandResult> ActivateAsync(int contextId, CancellationToken cancellationToken = default)
    {
        ValidateContextId(contextId);
        return client.ExecuteAsync($"AT+CGACT=1,{contextId}", TimeSpan.FromSeconds(60), cancellationToken);
    }

    public Task<AtCommandResult> DeactivateAsync(int contextId, CancellationToken cancellationToken = default)
    {
        ValidateContextId(contextId);
        return client.ExecuteAsync($"AT+CGACT=0,{contextId}", TimeSpan.FromSeconds(60), cancellationToken);
    }

    private static void Validate(PdpContextSettings settings)
    {
        ValidateContextId(settings.ContextId);
        if (string.IsNullOrWhiteSpace(settings.Apn)) throw new ArgumentException("APNを入力してください。", nameof(settings));
        if (settings.Apn.Length > 100) throw new ArgumentException("APNは100文字以内で入力してください。", nameof(settings));
        if (settings.Authentication != PdpAuthentication.None && string.IsNullOrWhiteSpace(settings.UserName))
            throw new ArgumentException("認証を使用する場合はユーザー名を入力してください。", nameof(settings));
    }

    private static void ValidateContextId(int contextId)
    {
        if (contextId is < 1 or > 16) throw new ArgumentOutOfRangeException(nameof(contextId), "CIDは1～16で指定してください。");
    }

    private static string Quote(string value)
    {
        if (value.ContainsAny('\r', '\n', '"')) throw new ArgumentException("改行とダブルクォートは入力できません。");
        return value;
    }
}

file static class StringExtensions
{
    public static bool ContainsAny(this string value, params char[] chars) => value.IndexOfAny(chars) >= 0;
}
