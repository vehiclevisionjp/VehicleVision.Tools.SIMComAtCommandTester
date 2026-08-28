using VehicleVision.SimComAt;

var tests = new (string Name, Func<Task> Run)[]
{
    ("OK応答を成功として扱う", SuccessfulResponse),
    ("ERROR応答を失敗として扱う", ErrorResponse),
    ("ATプレフィックスを補完する", PrefixIsAdded),
    ("応答なしをタイムアウトとして扱う", TimeoutResponse),
    ("主要系列のプロファイルが存在する", AllProfilesExist),
    ("型番から5Gプロファイルを判定する", DetectsFiveGProfile),
    ("APNとPAP認証を設定する", ConfiguresPdpContext),
    ("認証情報をログ用文字列から除去する", RedactsCredentials),
    ("APNへのコマンド注入を拒否する", RejectsUnsafeApn),
    ("SMSプロンプト後に本文とCtrl+Zを送信する", SendsSmsPayload),
    ("日本語SMSをUCS2へ変換する", SendsUcs2Sms),
    ("系列別GNSSコマンドを選択する", SelectsGnssCommands)
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        await test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

return failed == 0 ? 0 : 1;

static async Task SuccessfulResponse()
{
    await using var transport = new FakeTransport("AT+CSQ", "+CSQ: 20,99", "OK");
    await transport.OpenAsync();
    var result = await new AtCommandClient(transport).ExecuteAsync("AT+CSQ");
    Assert(result.IsSuccess && !result.TimedOut, "成功判定が不正です。");
    Assert(result.Lines.SequenceEqual(["+CSQ: 20,99", "OK"]), "応答行が一致しません。");
}

static async Task ErrorResponse()
{
    await using var transport = new FakeTransport("+CME ERROR: operation not allowed");
    await transport.OpenAsync();
    var result = await new AtCommandClient(transport).ExecuteAsync("AT+CFUN=1");
    Assert(!result.IsSuccess && !result.TimedOut, "エラー判定が不正です。");
}

static async Task PrefixIsAdded()
{
    await using var transport = new FakeTransport("OK");
    await transport.OpenAsync();
    var result = await new AtCommandClient(transport).ExecuteAsync("+CGMI");
    Assert(result.Command == "AT+CGMI", "ATプレフィックスが補完されません。");
    Assert(transport.Commands.Single() == "AT+CGMI", "補完後のコマンドが送信されていません。");
}

static async Task TimeoutResponse()
{
    await using var transport = new FakeTransport();
    await transport.OpenAsync();
    var result = await new AtCommandClient(transport).ExecuteAsync("AT", TimeSpan.FromMilliseconds(5));
    Assert(result.TimedOut && !result.IsSuccess, "タイムアウト判定が不正です。");
}

static Task AllProfilesExist()
{
    Assert(ModemProfiles.All.Count >= 10, "系列プロファイルが不足しています。");
    Assert(ModemProfiles.All.All(x => x.Commands.Count > 0), "コマンドのないプロファイルがあります。");
    Assert(ModemProfiles.Match("SIM7600JC-H").Family == SimComFamily.Sim7500_7600, "SIM7600を判定できません。");
    Assert(ModemProfiles.Match("UNKNOWN").Family == SimComFamily.Generic, "未知の型番が汎用になりません。");
    return Task.CompletedTask;
}

static async Task DetectsFiveGProfile()
{
    await using var transport = new FakeTransport(
        "OK",
        "SIMCOM INCORPORATED", "OK",
        "SIM8262E-M2", "OK",
        "LE20B04SIM8262", "OK");
    await transport.OpenAsync();
    var identity = await ModemDetector.DetectAsync(new AtCommandClient(transport));
    Assert(identity.Profile.Family == SimComFamily.Sim82xx_83xx, "5G系列を判定できません。");
    Assert(identity.Profile.Supports(ModemCapability.FiveG), "5G能力が設定されていません。");
}

static async Task ConfiguresPdpContext()
{
    await using var transport = new FakeTransport("OK", "OK");
    await transport.OpenAsync();
    var service = new PdpContextService(new AtCommandClient(transport));
    var result = await service.ConfigureAsync(new PdpContextSettings(1, "example.apn", PdpType.IPV4V6, PdpAuthentication.Pap, "user", "secret"));
    Assert(result.IsSuccess, "PDP設定が成功しません。");
    Assert(transport.Commands[0] == "AT+CGDCONT=1,\"IPV4V6\",\"example.apn\"", "APNコマンドが不正です。");
    Assert(transport.Commands[1] == "AT+CGAUTH=1,1,\"secret\",\"user\"", "認証コマンドが不正です。");
    Assert(result.Steps[1].Command == "AT+CGAUTH=1,1,\"***\",\"***\"", "結果に資格情報が残っています。");
}

static Task RedactsCredentials()
{
    var redacted = AtCommandRedactor.Redact("AT+CGAUTH=1,2,\"alice\",\"password\"");
    Assert(redacted == "AT+CGAUTH=1,2,\"***\",\"***\"", "資格情報がマスクされません。");
    var response = AtCommandRedactor.Redact("+CGAUTH: 1,1,\"alice\"");
    Assert(response == "+CGAUTH: 1,1,\"***\"", "応答の資格情報がマスクされません。");
    return Task.CompletedTask;
}

static async Task RejectsUnsafeApn()
{
    await using var transport = new FakeTransport();
    await transport.OpenAsync();
    try
    {
        await new PdpContextService(new AtCommandClient(transport)).ConfigureAsync(
            new PdpContextSettings(1, "apn\"\rAT+CFUN=1"));
        throw new InvalidOperationException("危険なAPNが受理されました。");
    }
    catch (ArgumentException)
    {
        Assert(transport.Commands.Count == 0, "検証前にコマンドが送信されました。");
    }
}

static async Task SendsSmsPayload()
{
    await using var transport = new FakeTransport("OK", "OK", "+CMGS: 42", "OK");
    await transport.OpenAsync();
    var result = await new SmsService(new AtCommandClient(transport)).SendAsync(new SmsMessage("+819012345678", "hello"));
    Assert(result.IsSuccess, "SMS送信が成功しません。");
    Assert(transport.Commands[^1] == "AT+CMGS=\"+819012345678\"", "CMGSコマンドが不正です。");
    Assert(transport.RawWrites.Count == 1, "SMS本文が送信されていません。");
    Assert(transport.RawWrites[0].SequenceEqual(System.Text.Encoding.ASCII.GetBytes("hello").Append((byte)0x1A)), "本文または終端文字が不正です。");
    Assert(transport.RawTraceTexts[0] == "[SMS本文 5文字]", "本文がログへ露出しています。");
}

static async Task SendsUcs2Sms()
{
    await using var transport = new FakeTransport("OK", "OK", "OK", "+CMGS: 7", "OK");
    await transport.OpenAsync();
    var result = await new SmsService(new AtCommandClient(transport)).SendAsync(
        new SmsMessage("+8190", "テスト", SmsTextEncoding.Ucs2));
    Assert(result.IsSuccess, "UCS2 SMS送信が成功しません。");
    Assert(transport.Commands.Contains("AT+CSCS=\"UCS2\""), "UCS2文字セットが設定されていません。");
    Assert(transport.Commands[^1] == "AT+CMGS=\"002B0038003100390030\"", "送信先のUCS2変換が不正です。");
    var payload = System.Text.Encoding.ASCII.GetString(transport.RawWrites[0][..^1]);
    Assert(payload == "30C630B930C8", "本文のUCS2変換が不正です。");
}

static Task SelectsGnssCommands()
{
    Assert(GnssService.TryGetCommands(SimComFamily.Sim7000, out var lpwa) && lpwa.Information == "AT+CGNSINF", "SIM7000 GNSSが不正です。");
    Assert(GnssService.TryGetCommands(SimComFamily.A76xx, out var cat1) && cat1.PowerOn == "AT+CGNSSPWR=1", "A76xx GNSSが不正です。");
    Assert(GnssService.TryGetCommands(SimComFamily.Sim7500_7600, out var lte) && lte.Information == "AT+CGPSINFO", "SIM7600 GNSSが不正です。");
    return Task.CompletedTask;
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

file sealed class FakeTransport(params string[] lines) : IAtTransport
{
    private readonly Queue<string> _lines = new(lines);
    public bool IsOpen { get; private set; }
    public List<string> Commands { get; } = [];
    public List<byte[]> RawWrites { get; } = [];
    public List<string> RawTraceTexts { get; } = [];
    public event EventHandler<AtTraceEntry>? Trace;

    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        IsOpen = true;
        return Task.CompletedTask;
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        IsOpen = false;
        return Task.CompletedTask;
    }

    public Task WriteLineAsync(string command, CancellationToken cancellationToken = default)
    {
        Commands.Add(command);
        Trace?.Invoke(this, new AtTraceEntry(DateTimeOffset.Now, true, AtCommandRedactor.Redact(command)));
        return Task.CompletedTask;
    }

    public Task WriteRawAsync(ReadOnlyMemory<byte> data, string traceText, CancellationToken cancellationToken = default)
    {
        RawWrites.Add(data.ToArray());
        RawTraceTexts.Add(traceText);
        Trace?.Invoke(this, new AtTraceEntry(DateTimeOffset.Now, true, traceText));
        return Task.CompletedTask;
    }

    public Task<string?> ReadLineAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var line = _lines.Count > 0 ? _lines.Dequeue() : null;
        if (line is not null) Trace?.Invoke(this, new AtTraceEntry(DateTimeOffset.Now, false, line));
        return Task.FromResult(line);
    }

    public Task<string?> ReadUntilAsync(string marker, TimeSpan timeout, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(marker);

    public ValueTask DisposeAsync()
    {
        IsOpen = false;
        return ValueTask.CompletedTask;
    }
}
