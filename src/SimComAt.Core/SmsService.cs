using System.Text;

namespace VehicleVision.SimComAt;

public sealed class SmsService(AtCommandClient client)
{
    public async Task<AtWorkflowResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        Validate(message);
        var steps = new List<AtCommandResult>();
        steps.Add(await client.ExecuteAsync("AT+CMGF=1", cancellationToken: cancellationToken));
        if (!steps[^1].IsSuccess) return new(steps);

        var isUcs2 = message.Encoding == SmsTextEncoding.Ucs2;
        steps.Add(await client.ExecuteAsync(isUcs2 ? "AT+CSCS=\"UCS2\"" : "AT+CSCS=\"GSM\"", cancellationToken: cancellationToken));
        if (!steps[^1].IsSuccess) return new(steps);

        if (isUcs2)
        {
            steps.Add(await client.ExecuteAsync("AT+CSMP=17,167,0,8", cancellationToken: cancellationToken));
            if (!steps[^1].IsSuccess) return new(steps);
        }

        var destination = isUcs2 ? ToUcs2Hex(message.Destination) : message.Destination;
        var payload = isUcs2 ? Encoding.ASCII.GetBytes(ToUcs2Hex(message.Text)) : Encoding.ASCII.GetBytes(message.Text);
        steps.Add(await client.ExecutePromptAsync(
            $"AT+CMGS=\"{destination}\"", ">", payload,
            payloadTraceText: $"[SMS本文 {message.Text.Length}文字]",
            cancellationToken: cancellationToken));
        return new(steps);
    }

    private static void Validate(SmsMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Destination) ||
            message.Destination.Any(x => !char.IsAsciiDigit(x) && x != '+'))
            throw new ArgumentException("送信先は+と数字のみで入力してください。", nameof(message));
        if (message.Destination.Length > 20) throw new ArgumentException("送信先が長すぎます。", nameof(message));
        if (string.IsNullOrEmpty(message.Text)) throw new ArgumentException("SMS本文を入力してください。", nameof(message));
        if (message.Encoding == SmsTextEncoding.Gsm && message.Text.Any(x => !char.IsAscii(x)))
            throw new ArgumentException("日本語などの非ASCII文字を送る場合はUCS2を選択してください。", nameof(message));
        if (message.Encoding == SmsTextEncoding.Gsm && message.Text.Length > 160)
            throw new ArgumentException("GSM SMS本文は160文字以内で入力してください。", nameof(message));
        if (message.Encoding == SmsTextEncoding.Ucs2 && message.Text.Length > 70)
            throw new ArgumentException("UCS2 SMS本文は70文字以内で入力してください。", nameof(message));
    }

    private static string ToUcs2Hex(string value)
    {
        var bytes = Encoding.BigEndianUnicode.GetBytes(value);
        return Convert.ToHexString(bytes);
    }
}
