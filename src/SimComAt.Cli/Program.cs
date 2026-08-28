using System.Text;
using System.Reflection;
using VehicleVision.SimComAt;

Console.OutputEncoding = Encoding.UTF8;
Console.CancelKeyPress += (_, eventArgs) => eventArgs.Cancel = true;

var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "development";
Console.WriteLine($"SIMCom AT Command Tester v{version} - CLI");
Console.WriteLine();

var profile = SelectProfile();
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
    if (profile.Family == SimComFamily.Generic)
        profile = await DetectAsync(client);
    await RunMenuAsync(profile, client);
    return 0;
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
{
    Console.Error.WriteLine($"通信エラー: {ex.Message}");
    return 1;
}

static ModemProfile SelectProfile()
{
    var profiles = ModemProfiles.All;
    Console.WriteLine("対象モジュール:");
    for (var i = 0; i < profiles.Count; i++) Console.WriteLine($"  {i + 1}. {profiles[i].DisplayName}");
    return profiles[ReadInt("選択 [1]: ", 1, 1, profiles.Count) - 1];
}

static string SelectPort(IReadOnlyList<string> ports)
{
    Console.WriteLine("シリアルポート:");
    for (var i = 0; i < ports.Count; i++) Console.WriteLine($"  {i + 1}. {ports[i]}");
    return ports[ReadInt("選択: ", 1, 1, ports.Count) - 1];
}

static async Task<ModemProfile> DetectAsync(AtCommandClient client)
{
    Console.WriteLine("機種を自動判定しています...");
    var identity = await ModemDetector.DetectAsync(client);
    Console.WriteLine($"検出: {identity.Manufacturer} / {identity.Model} / {identity.Revision}");
    Console.WriteLine($"プロファイル: {identity.Profile.DisplayName}");
    return identity.Profile;
}

static async Task RunMenuAsync(ModemProfile profile, AtCommandClient client)
{
    while (true)
    {
        Console.WriteLine();
        Console.WriteLine($"プロファイル: {profile.DisplayName}");
        Console.WriteLine("1. 定義済みコマンド  2. AT直接入力  3. 一括診断  4. APN・認証設定");
        Console.WriteLine("5. PSアタッチ        6. PDP有効化    7. PDP無効化  8. 機種再判定");
        Console.WriteLine("9. SMS送信           10. GNSS操作     11. HTTP実行   12. MQTT操作  0. 終了");
        try
        {
            switch (ReadInt("選択: ", 0, 0, 12))
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
                    foreach (var definition in profile.Commands.Where(x =>
                                 !x.IsParameterized && x.Risk == AtCommandRisk.ReadOnly &&
                                 x.Category is "基本" or "端末情報" or "SIM" or "ネットワーク"))
                        await ExecuteAndPrintAsync(client, definition.Command);
                    break;
                case 4:
                    await ConfigurePdpAsync(client);
                    break;
                case 5:
                    PrintResult(await new PdpContextService(client).AttachAsync(), "PSアタッチ");
                    break;
                case 6:
                    PrintResult(await new PdpContextService(client).ActivateAsync(ReadInt("CID [1]: ", 1, 1, 16)), "PDP有効化");
                    break;
                case 7:
                    PrintResult(await new PdpContextService(client).DeactivateAsync(ReadInt("CID [1]: ", 1, 1, 16)), "PDP無効化");
                    break;
                case 8:
                    profile = await DetectAsync(client);
                    break;
                case 9:
                    await SendSmsAsync(client);
                    break;
                case 10:
                    await RunGnssAsync(profile, client);
                    break;
                case 11:
                    await RunHttpAsync(client);
                    break;
                case 12:
                    await RunMqttAsync(client);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"操作失敗: {ex.Message}");
        }
    }
}

static async Task RunPresetAsync(ModemProfile profile, AtCommandClient client)
{
    Console.WriteLine();
    var commands = profile.Commands.Where(x => !x.IsParameterized).ToArray();
    for (var i = 0; i < commands.Length; i++)
        Console.WriteLine($"  {i + 1,2}. [{commands[i].Category}] {commands[i].DisplayName,-18} {commands[i].Command} [{RiskLabel(commands[i].Risk)}]");
    var selected = ReadInt("選択 (0で戻る): ", 0, 0, commands.Length);
    if (selected == 0) return;
    var definition = commands[selected - 1];
    if (definition.Risk == AtCommandRisk.Destructive && !Confirm($"{definition.DisplayName}を実行しますか？")) return;
    await ExecuteAndPrintAsync(client, definition.Command);
}

static async Task ConfigurePdpAsync(AtCommandClient client)
{
    var cid = ReadInt("CID [1]: ", 1, 1, 16);
    Console.Write("APN: ");
    var apn = Console.ReadLine()?.Trim() ?? string.Empty;
    Console.WriteLine("PDPタイプ: 1. IP  2. IPV6  3. IPV4V6");
    var pdpType = (PdpType)(ReadInt("選択 [1]: ", 1, 1, 3) - 1);
    Console.WriteLine("認証: 0. なし  1. PAP  2. CHAP  3. PAPまたはCHAP");
    var authentication = (PdpAuthentication)ReadInt("選択 [0]: ", 0, 0, 3);
    var user = string.Empty;
    var password = string.Empty;
    if (authentication != PdpAuthentication.None)
    {
        Console.Write("ユーザー名: ");
        user = Console.ReadLine() ?? string.Empty;
        password = ReadSecret("パスワード: ");
    }

    var result = await new PdpContextService(client).ConfigureAsync(
        new PdpContextSettings(cid, apn, pdpType, authentication, user, password));
    Console.WriteLine(result.IsSuccess ? "APN・認証設定が完了しました。" : $"設定失敗: {result.FailedStep?.RawResponse}");
}

static async Task SendSmsAsync(AtCommandClient client)
{
    Console.Write("送信先 (+8190...): ");
    var destination = Console.ReadLine()?.Trim() ?? string.Empty;
    Console.WriteLine("文字コード: 1. GSM/ASCII  2. UCS2/日本語");
    var encoding = ReadInt("選択 [1]: ", 1, 1, 2) == 1 ? SmsTextEncoding.Gsm : SmsTextEncoding.Ucs2;
    Console.Write("本文: ");
    var body = Console.ReadLine() ?? string.Empty;
    if (!Confirm($"{destination}へSMSを送信しますか？")) return;
    var result = await new SmsService(client).SendAsync(new SmsMessage(destination, body, encoding));
    Console.WriteLine(result.IsSuccess ? "SMS送信が完了しました。" : $"SMS送信失敗: {result.FailedStep?.RawResponse}");
}

static async Task RunGnssAsync(ModemProfile profile, AtCommandClient client)
{
    if (!profile.Supports(ModemCapability.Gnss))
    {
        Console.WriteLine("選択中のプロファイルはGNSS非対応です。");
        return;
    }
    Console.WriteLine("1. 電源ON  2. 測位情報取得  3. 電源OFF  0. 戻る");
    var service = new GnssService(client, profile);
    switch (ReadInt("選択: ", 0, 0, 3))
    {
        case 1: PrintResult(await service.PowerOnAsync(), "GNSS電源ON"); break;
        case 2: PrintResult(await service.GetInformationAsync(), "GNSS情報取得"); break;
        case 3: PrintResult(await service.PowerOffAsync(), "GNSS電源OFF"); break;
    }
}

static async Task RunHttpAsync(AtCommandClient client)
{
    Console.Write("URL: ");
    var url = Console.ReadLine()?.Trim() ?? string.Empty;
    Console.WriteLine("1. GET  2. POST");
    var method = ReadInt("選択 [1]: ", 1, 1, 2) == 1 ? HttpRequestMethod.Get : HttpRequestMethod.Post;
    var body = string.Empty;
    var contentType = "application/json";
    if (method == HttpRequestMethod.Post)
    {
        Console.Write($"Content-Type [{contentType}]: ");
        contentType = Console.ReadLine() is { Length: > 0 } value ? value : contentType;
        Console.Write("本文: ");
        body = Console.ReadLine() ?? string.Empty;
    }
    var result = await new HttpService(client).SendAsync(new HttpRequestSettings(url, method, body, contentType));
    Console.WriteLine(result.StatusCode is null
        ? $"HTTP失敗: {result.Workflow.FailedStep?.RawResponse}"
        : $"HTTP {result.StatusCode} ({result.ContentLength} bytes)");
    if (result.Content.Length > 0) Console.WriteLine(result.Content);
}

static async Task RunMqttAsync(AtCommandClient client)
{
    var service = new MqttService(client);
    Console.WriteLine("1. 接続  2. Publish  3. 切断  0. 戻る");
    switch (ReadInt("選択: ", 0, 0, 3))
    {
        case 1:
            Console.Write("ブローカー (tcp://host:1883): ");
            var broker = Console.ReadLine()?.Trim() ?? string.Empty;
            Console.Write("クライアントID: ");
            var clientId = Console.ReadLine()?.Trim() ?? string.Empty;
            Console.Write("ユーザー名 (認証なしは空): ");
            var user = Console.ReadLine() ?? string.Empty;
            var password = user.Length > 0 ? ReadSecret("パスワード: ") : string.Empty;
            var connected = await service.ConnectAsync(new MqttConnectionSettings(broker, clientId, user, password));
            Console.WriteLine(connected.IsSuccess ? "MQTT接続完了" : $"MQTT接続失敗: {connected.FailedStep?.RawResponse}");
            break;
        case 2:
            Console.Write("トピック: ");
            var topic = Console.ReadLine() ?? string.Empty;
            Console.Write("ペイロード: ");
            var payload = Console.ReadLine() ?? string.Empty;
            var published = await service.PublishAsync(0, new MqttPublishSettings(topic, payload));
            Console.WriteLine(published.IsSuccess ? "MQTT publish完了" : $"MQTT publish失敗: {published.FailedStep?.RawResponse}");
            break;
        case 3:
            var disconnected = await service.DisconnectAsync(0);
            Console.WriteLine(disconnected.IsSuccess ? "MQTT切断完了" : $"MQTT切断失敗: {disconnected.FailedStep?.RawResponse}");
            break;
    }
}

static async Task ExecuteAndPrintAsync(AtCommandClient client, string command)
{
    try
    {
        PrintResult(await client.ExecuteAsync(command), command);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"実行失敗: {ex.Message}");
    }
}

static void PrintResult(AtCommandResult result, string operation) => Console.WriteLine(result.TimedOut
    ? $"{operation}: タイムアウト ({result.Elapsed.TotalSeconds:F1}秒)"
    : result.IsSuccess ? $"{operation}: 成功" : $"{operation}: エラー応答");

static string ReadSecret(string prompt)
{
    Console.Write(prompt);
    if (Console.IsInputRedirected) return Console.ReadLine() ?? string.Empty;
    var value = new StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return value.ToString(); }
        if (key.Key == ConsoleKey.Backspace && value.Length > 0) { value.Length--; continue; }
        if (!char.IsControl(key.KeyChar)) value.Append(key.KeyChar);
    }
}

static bool Confirm(string message)
{
    Console.Write($"{message} [y/N]: ");
    return string.Equals(Console.ReadLine(), "y", StringComparison.OrdinalIgnoreCase);
}

static string RiskLabel(AtCommandRisk risk) => risk switch
{
    AtCommandRisk.ReadOnly => "照会",
    AtCommandRisk.Configuration => "設定",
    AtCommandRisk.Connectivity => "接続変更",
    AtCommandRisk.Destructive => "要注意",
    _ => risk.ToString()
};

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
