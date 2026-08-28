using VehicleVision.SimComAt;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.CancelKeyPress += (_, eventArgs) => eventArgs.Cancel = true;

Console.WriteLine("SIMCom AT Command Tester - CLI");
Console.WriteLine();

var model = SelectModel();
var profile = ModemProfiles.Get(model);
var ports = SerialPortDiscovery.GetPortNames();
if (ports.Count == 0)
{
    Console.Error.WriteLine("利用可能なシリアルポートがありません。");
    return 2;
}

var port = SelectPort(ports);
var baudRate = ReadInt($"ボーレート [{profile.DefaultBaudRate}]: ", profile.DefaultBaudRate);

await using var transport = new SerialAtTransport(new SerialConnectionOptions(port, baudRate));
var client = new AtCommandClient(transport);
client.Trace += (_, entry) =>
{
    var marker = entry.IsTransmit ? ">>" : "<<";
    Console.WriteLine($"{entry.Timestamp:HH:mm:ss.fff} {marker} {entry.Text}");
};

try
{
    await client.ConnectAsync();
    Console.WriteLine($"{profile.DisplayName} / {port} ({baudRate} bps) に接続しました。");
    await RunMenuAsync(profile, client);
    return 0;
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
{
    Console.Error.WriteLine($"通信エラー: {ex.Message}");
    return 1;
}

static SimComModel SelectModel()
{
    var profiles = ModemProfiles.All.ToArray();
    Console.WriteLine("対象モジュール:");
    for (var i = 0; i < profiles.Length; i++) Console.WriteLine($"  {i + 1}. {profiles[i].DisplayName}");
    var selected = ReadInt("選択: ", 1, 1, profiles.Length);
    return profiles[selected - 1].Model;
}

static string SelectPort(IReadOnlyList<string> ports)
{
    Console.WriteLine("シリアルポート:");
    for (var i = 0; i < ports.Count; i++) Console.WriteLine($"  {i + 1}. {ports[i]}");
    return ports[ReadInt("選択: ", 1, 1, ports.Count) - 1];
}

static async Task RunMenuAsync(ModemProfile profile, AtCommandClient client)
{
    while (true)
    {
        Console.WriteLine();
        Console.WriteLine("1. 定義済みコマンド  2. AT直接入力  3. 一括診断  0. 終了");
        switch (ReadInt("選択: ", 0, 0, 3))
        {
            case 0:
                await client.DisconnectAsync();
                return;
            case 1:
                await RunPresetAsync(profile, client);
                break;
            case 2:
                Console.Write("ATコマンド: ");
                var command = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(command)) await ExecuteAndPrintAsync(client, command);
                break;
            case 3:
                foreach (var definition in profile.Commands.Where(x => x.Category is "基本" or "端末情報" or "SIM" or "ネットワーク"))
                    await ExecuteAndPrintAsync(client, definition.Command);
                break;
        }
    }
}

static async Task RunPresetAsync(ModemProfile profile, AtCommandClient client)
{
    Console.WriteLine();
    for (var i = 0; i < profile.Commands.Count; i++)
        Console.WriteLine($"  {i + 1,2}. [{profile.Commands[i].Category}] {profile.Commands[i].DisplayName,-16} {profile.Commands[i].Command}");
    var selected = ReadInt("選択 (0で戻る): ", 0, 0, profile.Commands.Count);
    if (selected > 0) await ExecuteAndPrintAsync(client, profile.Commands[selected - 1].Command);
}

static async Task ExecuteAndPrintAsync(AtCommandClient client, string command)
{
    try
    {
        var result = await client.ExecuteAsync(command);
        Console.WriteLine(result.TimedOut
            ? $"タイムアウト ({result.Elapsed.TotalSeconds:F1}秒)"
            : result.IsSuccess ? "成功" : "エラー応答");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"実行失敗: {ex.Message}");
    }
}

static int ReadInt(string prompt, int defaultValue, int min = int.MinValue, int max = int.MaxValue)
{
    while (true)
    {
        Console.Write(prompt);
        var text = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(text)) return defaultValue;
        if (int.TryParse(text, out var value) && value >= min && value <= max) return value;
        Console.WriteLine($"{min}～{max} の数値を入力してください。");
    }
}
