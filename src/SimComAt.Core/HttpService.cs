using System.Globalization;
using System.Text;

namespace VehicleVision.SimComAt;

public sealed class HttpService(AtCommandClient client)
{
    public async Task<HttpWorkflowResult> SendAsync(HttpRequestSettings settings, CancellationToken cancellationToken = default)
    {
        Validate(settings);
        var steps = new List<AtCommandResult>();
        var initialized = false;
        try
        {
            var init = await client.ExecuteAsync("AT+HTTPINIT", cancellationToken: cancellationToken);
            if (!init.IsSuccess)
            {
                await client.ExecuteAsync("AT+HTTPTERM", cancellationToken: cancellationToken);
                init = await client.ExecuteAsync("AT+HTTPINIT", cancellationToken: cancellationToken);
            }
            steps.Add(init);
            if (!init.IsSuccess) return new(new(steps), null, null, string.Empty);
            initialized = true;

            if (!await AddStepAsync(steps, $"AT+HTTPPARA=\"CID\",{settings.ContextId}", cancellationToken))
                return new(new(steps), null, null, string.Empty);
            if (!await AddStepAsync(steps, $"AT+HTTPPARA=\"URL\",\"{Quote(settings.Url)}\"", cancellationToken))
                return new(new(steps), null, null, string.Empty);

            if (settings.Method == HttpRequestMethod.Post)
            {
                if (!await AddStepAsync(steps, $"AT+HTTPPARA=\"CONTENT\",\"{Quote(settings.ContentType)}\"", cancellationToken))
                    return new(new(steps), null, null, string.Empty);
                var body = Encoding.UTF8.GetBytes(settings.Body);
                var data = await client.ExecutePromptAsync(
                    $"AT+HTTPDATA={body.Length},{(int)(settings.Timeout ?? TimeSpan.FromSeconds(60)).TotalMilliseconds}",
                    "DOWNLOAD", body, terminator: null, payloadTraceText: $"[HTTP本文 {body.Length} bytes]",
                    responseTimeout: settings.Timeout, cancellationToken: cancellationToken);
                steps.Add(data);
                if (!data.IsSuccess) return new(new(steps), null, null, string.Empty);
            }

            var action = await client.ExecuteAsync($"AT+HTTPACTION={(int)settings.Method}", cancellationToken: cancellationToken);
            steps.Add(action);
            if (!action.IsSuccess) return new(new(steps), null, null, string.Empty);

            var urc = await client.WaitForUnsolicitedAsync("+HTTPACTION:", settings.Timeout ?? TimeSpan.FromSeconds(120), cancellationToken);
            steps.Add(urc);
            if (!urc.IsSuccess || !TryParseAction(urc.Lines, out var status, out var length))
                return new(new(steps), null, null, string.Empty);

            var content = string.Empty;
            if (length > 0)
            {
                var read = await client.ExecuteAsync($"AT+HTTPREAD=0,{length}", settings.Timeout, cancellationToken);
                steps.Add(read);
                content = string.Join(Environment.NewLine, read.DataLines.Where(x => !x.StartsWith("+HTTPREAD:", StringComparison.Ordinal)));
            }
            return new(new(steps), status, length, content);
        }
        finally
        {
            if (initialized)
            {
                try { await client.ExecuteAsync("AT+HTTPTERM", cancellationToken: CancellationToken.None); }
                catch (Exception) { /* Preserve the original workflow result/error. */ }
            }
        }
    }

    private async Task<bool> AddStepAsync(List<AtCommandResult> steps, string command, CancellationToken cancellationToken)
    {
        var result = await client.ExecuteAsync(command, cancellationToken: cancellationToken);
        steps.Add(result);
        return result.IsSuccess;
    }

    private static bool TryParseAction(IEnumerable<string> lines, out int status, out int length)
    {
        status = 0;
        length = 0;
        var line = lines.LastOrDefault(x => x.StartsWith("+HTTPACTION:", StringComparison.OrdinalIgnoreCase));
        if (line is null) return false;
        var values = line[(line.IndexOf(':') + 1)..].Split(',', StringSplitOptions.TrimEntries);
        return values.Length >= 3 &&
               int.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out status) &&
               int.TryParse(values[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out length);
    }

    private static void Validate(HttpRequestSettings settings)
    {
        if (!Uri.TryCreate(settings.Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new ArgumentException("HTTPまたはHTTPSの絶対URLを入力してください。", nameof(settings));
        if (settings.ContextId is < 1 or > 16) throw new ArgumentOutOfRangeException(nameof(settings), "CIDは1～16で指定してください。");
        if (settings.Method == HttpRequestMethod.Post && Encoding.UTF8.GetByteCount(settings.Body) > 1024 * 1024)
            throw new ArgumentException("HTTP本文は1 MiB以下にしてください。", nameof(settings));
        Quote(settings.Url);
        Quote(settings.ContentType);
    }

    private static string Quote(string value)
    {
        if (value.IndexOfAny(['\r', '\n', '"']) >= 0) throw new ArgumentException("改行とダブルクォートは入力できません。");
        return value;
    }
}
