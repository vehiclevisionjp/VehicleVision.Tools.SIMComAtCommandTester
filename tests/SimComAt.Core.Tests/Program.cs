using VehicleVision.SimComAt;

var tests = new (string Name, Func<Task> Run)[]
{
    ("OK応答を成功として扱う", SuccessfulResponse),
    ("ERROR応答を失敗として扱う", ErrorResponse),
    ("ATプレフィックスを補完する", PrefixIsAdded),
    ("応答なしをタイムアウトとして扱う", TimeoutResponse),
    ("全対象モデルのプロファイルが存在する", AllProfilesExist)
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
    Assert(transport.LastCommand == "AT+CGMI", "補完後のコマンドが送信されていません。");
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
    var models = Enum.GetValues<SimComModel>();
    Assert(ModemProfiles.All.Count == models.Length, "プロファイル数がモデル数と一致しません。");
    foreach (var model in models) Assert(ModemProfiles.Get(model).Commands.Count > 0, $"{model} のコマンドがありません。");
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
    public string? LastCommand { get; private set; }
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
        LastCommand = command;
        Trace?.Invoke(this, new AtTraceEntry(DateTimeOffset.Now, true, command));
        return Task.CompletedTask;
    }

    public Task<string?> ReadLineAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var line = _lines.Count > 0 ? _lines.Dequeue() : null;
        if (line is not null) Trace?.Invoke(this, new AtTraceEntry(DateTimeOffset.Now, false, line));
        return Task.FromResult(line);
    }

    public ValueTask DisposeAsync()
    {
        IsOpen = false;
        return ValueTask.CompletedTask;
    }
}
