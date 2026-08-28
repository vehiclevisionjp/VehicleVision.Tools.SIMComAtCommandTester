namespace VehicleVision.SimComAt;

public static class AtCommandCatalog
{
    public static IReadOnlyList<AtCommandDefinition> All { get; } =
    [
        new("attention", "疎通確認", "AT", "基本", "モデムが応答するか確認"),
        new("info", "総合情報", "ATI", "基本", "製品情報を表示"),
        new("echo-off", "エコー無効", "ATE0", "基本", "コマンドエコーを無効化", Risk: AtCommandRisk.Configuration),
        new("echo-on", "エコー有効", "ATE1", "基本", "コマンドエコーを有効化", Risk: AtCommandRisk.Configuration),
        new("errors-text", "詳細エラー有効", "AT+CMEE=2", "基本", "詳細なエラー文字列を有効化", Risk: AtCommandRisk.Configuration),
        new("manufacturer", "メーカー", "AT+CGMI", "端末情報"),
        new("model", "モデル", "AT+CGMM", "端末情報"),
        new("revision", "ファームウェア", "AT+CGMR", "端末情報"),
        new("imei", "IMEI", "AT+CGSN", "端末情報"),
        new("serial-number", "シリアル番号", "AT+GSN", "端末情報"),
        new("capabilities", "対応コマンド一覧", "AT+CLAC", "端末情報", "モデムが公開するATコマンド一覧"),
        new("sim-status", "SIM状態", "AT+CPIN?", "SIM"),
        new("iccid", "ICCID", "AT+CCID", "SIM"),
        new("imsi", "IMSI", "AT+CIMI", "SIM"),
        new("own-number", "自局番号", "AT+CNUM", "SIM"),
        new("sim-inserted", "SIM挿入状態", "AT+CSMINS?", "SIM"),
        new("pin-retries", "PIN残回数", "AT+SPIC", "SIM"),
        new("signal", "電波強度", "AT+CSQ", "ネットワーク"),
        new("extended-signal", "拡張電波品質", "AT+CESQ", "ネットワーク"),
        new("registration-cs", "CS登録状態", "AT+CREG?", "ネットワーク"),
        new("registration-ps", "PS登録状態", "AT+CGREG?", "ネットワーク"),
        new("registration-eps", "LTE登録状態", "AT+CEREG?", "ネットワーク", RequiredCapability: ModemCapability.Lte),
        new("registration-5g", "5G登録状態", "AT+C5GREG?", "ネットワーク", RequiredCapability: ModemCapability.FiveG),
        new("operator", "通信事業者", "AT+COPS?", "ネットワーク"),
        new("operator-list", "事業者検索", "AT+COPS=?", "ネットワーク", "数分かかる場合があります", Risk: AtCommandRisk.Connectivity),
        new("system-info", "システム情報", "AT+CPSI?", "ネットワーク"),
        new("network-time", "ネットワーク時刻同期", "AT+CLTS?", "ネットワーク"),
        new("preferred-mode", "優先モード", "AT+CNMP?", "ネットワーク"),
        new("pdp-contexts", "PDPコンテキスト", "AT+CGDCONT?", "パケット通信", RequiredCapability: ModemCapability.PacketData),
        new("pdp-auth", "PDP認証設定", "AT+CGAUTH?", "パケット通信", RequiredCapability: ModemCapability.PacketData),
        new("attach-status", "PSアタッチ状態", "AT+CGATT?", "パケット通信", RequiredCapability: ModemCapability.PacketData),
        new("activation-status", "PDP有効状態", "AT+CGACT?", "パケット通信", RequiredCapability: ModemCapability.PacketData),
        new("pdp-address", "PDPアドレス", "AT+CGPADDR", "パケット通信", RequiredCapability: ModemCapability.PacketData),
        new("packet-errors", "パケットエラー", "AT+CEER", "パケット通信", RequiredCapability: ModemCapability.PacketData),
        new("define-pdp", "APN/PDP設定", "AT+CGDCONT={cid},\"{pdpType}\",\"{apn}\"", "パケット通信", "GUI/CLIのAPN設定を使用", ModemCapability.PacketData, AtCommandRisk.Configuration),
        new("define-auth", "PDP認証設定", "AT+CGAUTH={cid},{auth},\"{password}\",\"{user}\"", "パケット通信", "GUI/CLIのAPN設定を使用。SIMCom構文はパスワード、ユーザー名の順", ModemCapability.PacketData, AtCommandRisk.Configuration),
        new("attach", "PSアタッチ", "AT+CGATT=1", "パケット通信", RequiredCapability: ModemCapability.PacketData, Risk: AtCommandRisk.Connectivity),
        new("detach", "PSデタッチ", "AT+CGATT=0", "パケット通信", RequiredCapability: ModemCapability.PacketData, Risk: AtCommandRisk.Connectivity),
        new("sms-format", "SMS形式", "AT+CMGF?", "SMS", RequiredCapability: ModemCapability.Sms),
        new("sms-text-mode", "SMSテキストモード", "AT+CMGF=1", "SMS", RequiredCapability: ModemCapability.Sms, Risk: AtCommandRisk.Configuration),
        new("sms-storage", "SMS保存先", "AT+CPMS?", "SMS", RequiredCapability: ModemCapability.Sms),
        new("sms-center", "SMSセンター", "AT+CSCA?", "SMS", RequiredCapability: ModemCapability.Sms),
        new("sms-notification", "SMS通知設定", "AT+CNMI?", "SMS", RequiredCapability: ModemCapability.Sms),
        new("sms-list", "SMS一覧", "AT+CMGL=\"ALL\"", "SMS", RequiredCapability: ModemCapability.Sms),
        new("battery", "電源状態", "AT+CBC", "診断"),
        new("clock", "モデム時刻", "AT+CCLK?", "診断"),
        new("functionality", "機能レベル", "AT+CFUN?", "診断"),
        new("temperature", "温度", "AT+CPMUTEMP", "診断"),
        new("gnss-power", "GNSS電源状態", "AT+CGNSPWR?", "GNSS", RequiredCapability: ModemCapability.Gnss),
        new("gnss-info", "GNSS測位情報", "AT+CGNSINF", "GNSS", RequiredCapability: ModemCapability.Gnss),
        new("http-status", "HTTPサービス状態", "AT+HTTPSTATUS?", "HTTP", RequiredCapability: ModemCapability.Http),
        new("mqtt-status", "MQTT接続状態", "AT+CMQTTCONNECT?", "MQTT", RequiredCapability: ModemCapability.Mqtt),
        new("filesystem-list", "ファイル一覧", "AT+FSLS", "ファイル", RequiredCapability: ModemCapability.FileSystem),
        new("save-profile", "設定保存", "AT&W", "保守", "現在の設定を不揮発領域へ保存", Risk: AtCommandRisk.Destructive),
        new("reset-settings", "工場設定読込", "AT&F", "保守", "設定が変更されます", Risk: AtCommandRisk.Destructive),
        new("power-down", "電源OFF", "AT+CPOF", "保守", "モデムの電源を切ります", Risk: AtCommandRisk.Destructive)
    ];

    public static IReadOnlyList<AtCommandDefinition> For(ModemCapability capabilities) =>
        All.Where(x => x.RequiredCapability == ModemCapability.None ||
                       (capabilities & x.RequiredCapability) == x.RequiredCapability).ToArray();
}
