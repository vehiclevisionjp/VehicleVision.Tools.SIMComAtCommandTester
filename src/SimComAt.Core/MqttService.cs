using System.Text;

namespace VehicleVision.SimComAt;

public sealed class MqttService(AtCommandClient client)
{
    public async Task<AtWorkflowResult> ConnectAsync(MqttConnectionSettings settings, CancellationToken cancellationToken = default)
    {
        Validate(settings);
        var steps = new List<AtCommandResult>();
        if (!await ExecuteAndWaitAsync(steps, "AT+CMQTTSTART", "+CMQTTSTART:", cancellationToken)) return new(steps);
        if (!await AddStepAsync(steps, $"AT+CMQTTACCQ={settings.ClientIndex},\"{Quote(settings.ClientId)}\",0", cancellationToken)) return new(steps);

        var command = $"AT+CMQTTCONNECT={settings.ClientIndex},\"{Quote(settings.Broker)}\",{settings.KeepAliveSeconds},{(settings.CleanSession ? 1 : 0)}";
        if (!string.IsNullOrEmpty(settings.UserName))
            command += $",\"{Quote(settings.UserName)}\",\"{Quote(settings.Password)}\"";
        await ExecuteAndWaitAsync(steps, command, $"+CMQTTCONNECT: {settings.ClientIndex},", cancellationToken);
        return new(steps);
    }

    public async Task<AtWorkflowResult> PublishAsync(int clientIndex, MqttPublishSettings settings, CancellationToken cancellationToken = default)
    {
        ValidateClientIndex(clientIndex);
        if (string.IsNullOrEmpty(settings.Topic)) throw new ArgumentException("MQTTトピックを入力してください。", nameof(settings));
        if (settings.QualityOfService is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(settings), "QoSは0～2で指定してください。");
        var topic = Encoding.UTF8.GetBytes(settings.Topic);
        var payload = Encoding.UTF8.GetBytes(settings.Payload);
        var steps = new List<AtCommandResult>();

        var topicResult = await client.ExecutePromptAsync($"AT+CMQTTTOPIC={clientIndex},{topic.Length}", ">", topic,
            terminator: null, payloadTraceText: $"[MQTTトピック {topic.Length} bytes]", cancellationToken: cancellationToken);
        steps.Add(topicResult);
        if (!topicResult.IsSuccess) return new(steps);

        var payloadResult = await client.ExecutePromptAsync($"AT+CMQTTPAYLOAD={clientIndex},{payload.Length}", ">", payload,
            terminator: null, payloadTraceText: $"[MQTTペイロード {payload.Length} bytes]", cancellationToken: cancellationToken);
        steps.Add(payloadResult);
        if (!payloadResult.IsSuccess) return new(steps);

        await ExecuteAndWaitAsync(steps,
            $"AT+CMQTTPUB={clientIndex},{settings.QualityOfService},{(settings.Retain ? 1 : 0)}",
            $"+CMQTTPUB: {clientIndex},", cancellationToken);
        return new(steps);
    }

    public async Task<AtWorkflowResult> DisconnectAsync(int clientIndex, CancellationToken cancellationToken = default)
    {
        ValidateClientIndex(clientIndex);
        var steps = new List<AtCommandResult>();
        await ExecuteAndWaitAsync(steps, $"AT+CMQTTDISC={clientIndex},60", $"+CMQTTDISC: {clientIndex},", cancellationToken);
        await AddStepAsync(steps, $"AT+CMQTTREL={clientIndex}", cancellationToken);
        await ExecuteAndWaitAsync(steps, "AT+CMQTTSTOP", "+CMQTTSTOP:", cancellationToken);
        return new(steps);
    }

    private async Task<bool> ExecuteAndWaitAsync(List<AtCommandResult> steps, string command, string urcPrefix, CancellationToken cancellationToken)
    {
        if (!await AddStepAsync(steps, command, cancellationToken)) return false;
        var rawUrc = await client.WaitForUnsolicitedAsync(urcPrefix, TimeSpan.FromSeconds(90), cancellationToken);
        var succeeded = rawUrc.IsSuccess && TryGetResultCode(rawUrc.Lines.Last(), out var resultCode) && resultCode == 0;
        var urc = succeeded ? rawUrc : rawUrc with { IsSuccess = false };
        steps.Add(urc);
        return succeeded;
    }

    private async Task<bool> AddStepAsync(List<AtCommandResult> steps, string command, CancellationToken cancellationToken)
    {
        var result = await client.ExecuteAsync(command, cancellationToken: cancellationToken);
        steps.Add(result);
        return result.IsSuccess;
    }

    private static void Validate(MqttConnectionSettings settings)
    {
        ValidateClientIndex(settings.ClientIndex);
        if (!Uri.TryCreate(settings.Broker, UriKind.Absolute, out var uri) || uri.Scheme is not ("tcp" or "ssl") || uri.Port <= 0)
            throw new ArgumentException("ブローカーはtcp://host:portまたはssl://host:portで入力してください。", nameof(settings));
        if (string.IsNullOrWhiteSpace(settings.ClientId)) throw new ArgumentException("クライアントIDを入力してください。", nameof(settings));
        if (settings.KeepAliveSeconds is < 1 or > 64800) throw new ArgumentOutOfRangeException(nameof(settings), "Keep Aliveは1～64800秒で指定してください。");
        Quote(settings.Broker);
        Quote(settings.ClientId);
        Quote(settings.UserName);
        Quote(settings.Password);
    }

    private static void ValidateClientIndex(int clientIndex)
    {
        if (clientIndex is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(clientIndex), "クライアント番号は0または1です。");
    }

    private static bool TryGetResultCode(string line, out int resultCode)
    {
        resultCode = -1;
        var value = line[(line.LastIndexOf(',') + 1)..].Trim();
        if (!line.Contains(',')) value = line[(line.LastIndexOf(':') + 1)..].Trim();
        return int.TryParse(value, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out resultCode);
    }

    private static string Quote(string value)
    {
        if (value.IndexOfAny(['\r', '\n', '"']) >= 0) throw new ArgumentException("改行とダブルクォートは入力できません。");
        return value;
    }
}
