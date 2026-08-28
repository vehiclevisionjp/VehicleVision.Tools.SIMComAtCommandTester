namespace VehicleVision.SimComAt;

public static class ModemProfiles
{
    private static readonly AtCommandDefinition[] Common =
    [
        new("attention", "疎通確認", "AT", "基本", "モデムが応答するか確認"),
        new("echo-off", "エコー無効", "ATE0", "基本", "コマンドエコーを無効化"),
        new("manufacturer", "メーカー", "AT+CGMI", "端末情報"),
        new("model", "モデル", "AT+CGMM", "端末情報"),
        new("revision", "ファームウェア", "AT+CGMR", "端末情報"),
        new("imei", "IMEI", "AT+CGSN", "端末情報"),
        new("sim-status", "SIM状態", "AT+CPIN?", "SIM"),
        new("iccid", "ICCID", "AT+CCID", "SIM"),
        new("imsi", "IMSI", "AT+CIMI", "SIM"),
        new("signal", "電波強度", "AT+CSQ", "ネットワーク"),
        new("registration", "登録状態", "AT+CEREG?", "ネットワーク"),
        new("operator", "通信事業者", "AT+COPS?", "ネットワーク"),
        new("pdp-contexts", "PDPコンテキスト", "AT+CGDCONT?", "パケット通信"),
        new("attach-status", "PSアタッチ状態", "AT+CGATT?", "パケット通信"),
        new("clock", "モデム時刻", "AT+CCLK?", "診断"),
        new("errors", "詳細エラー有効", "AT+CMEE=2", "診断")
    ];

    private static readonly IReadOnlyDictionary<SimComModel, ModemProfile> Profiles =
        Enum.GetValues<SimComModel>().ToDictionary(
            model => model,
            model => new ModemProfile(model, DisplayName(model), 115200, Common));

    public static IReadOnlyCollection<ModemProfile> All { get; } = Profiles.Values.ToArray();

    public static ModemProfile Get(SimComModel model) => Profiles[model];

    private static string DisplayName(SimComModel model) => model switch
    {
        SimComModel.SIM7100Jx => "SIM7100Jx",
        SimComModel.SIM7600JC_H => "SIM7600JC-H",
        SimComModel.SIM7312G_M2 => "SIM7312G-M.2",
        SimComModel.SIM8262E_M2 => "SIM8262E-M2",
        _ => model.ToString()
    };
}
