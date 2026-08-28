namespace VehicleVision.SimComAt;

public static class ModemProfiles
{
    private const ModemCapability BasicPacket = ModemCapability.PacketData | ModemCapability.TcpIp;
    private const ModemCapability Internet = BasicPacket | ModemCapability.Http | ModemCapability.Mqtt | ModemCapability.FileSystem;

    private static readonly (string Id, string Name, SimComFamily Family, ModemCapability Caps, string[] Prefixes)[] Definitions =
    [
        ("generic", "自動検出 / 汎用SIMCom", SimComFamily.Generic, ModemCapability.Sms | BasicPacket, []),
        ("sim808-868", "SIM808 / SIM868シリーズ", SimComFamily.Sim800, ModemCapability.Gsm | ModemCapability.Sms | Internet | ModemCapability.Voice | ModemCapability.Gnss, ["SIM808", "SIM868"]),
        ("sim800", "SIM800 / SIM900シリーズ", SimComFamily.Sim800, ModemCapability.Gsm | ModemCapability.Sms | Internet | ModemCapability.Voice, ["SIM800", "SIM900"]),
        ("sim7000", "SIM7000シリーズ", SimComFamily.Sim7000, ModemCapability.Gsm | ModemCapability.LteM | ModemCapability.NbIot | ModemCapability.Sms | Internet | ModemCapability.Gnss, ["SIM7000"]),
        ("sim7020", "SIM7020 / SIM7022シリーズ", SimComFamily.Sim7020, ModemCapability.NbIot | Internet, ["SIM7020", "SIM7022"]),
        ("sim7070", "SIM7070 / SIM7080 / SIM7090シリーズ", SimComFamily.Sim7070, ModemCapability.LteM | ModemCapability.NbIot | ModemCapability.Sms | Internet | ModemCapability.Gnss, ["SIM7070", "SIM7080", "SIM7090"]),
        ("sim7100", "SIM7100シリーズ", SimComFamily.Sim7100, ModemCapability.Gsm | ModemCapability.Umts | ModemCapability.Lte | ModemCapability.Sms | Internet | ModemCapability.Gnss | ModemCapability.Voice, ["SIM7100"]),
        ("sim7500-7600", "SIM7500 / SIM7600 / SIM7800シリーズ", SimComFamily.Sim7500_7600, ModemCapability.Gsm | ModemCapability.Umts | ModemCapability.Lte | ModemCapability.Sms | Internet | ModemCapability.Gnss | ModemCapability.Voice, ["SIM7500", "SIM7600", "SIM7800"]),
        ("a76xx", "A76xxシリーズ", SimComFamily.A76xx, ModemCapability.Gsm | ModemCapability.Lte | ModemCapability.Sms | Internet | ModemCapability.Gnss | ModemCapability.Voice, ["A760", "A767", "A768"]),
        ("sim76xx", "SIM76xxシリーズ", SimComFamily.Sim76xx, ModemCapability.Lte | ModemCapability.Sms | Internet | ModemCapability.Gnss, ["SIM765", "SIM767"]),
        ("sim73xx", "SIM73xxシリーズ", SimComFamily.Sim73xx, ModemCapability.Lte | ModemCapability.Sms | Internet | ModemCapability.Gnss, ["SIM730", "SIM731", "SIM733"]),
        ("sim82xx-83xx", "SIM82xx / SIM83xx 5Gシリーズ", SimComFamily.Sim82xx_83xx, ModemCapability.Lte | ModemCapability.FiveG | ModemCapability.Sms | Internet | ModemCapability.Gnss | ModemCapability.Voice, ["SIM820", "SIM826", "SIM830", "SIM831", "SIM838"])
    ];

    public static IReadOnlyList<ModemProfile> All { get; } = Definitions
        .Select(x => new ModemProfile(x.Id, x.Name, x.Family, 115200, x.Caps, x.Prefixes, AtCommandCatalog.For(x.Caps, x.Family)))
        .ToArray();

    public static ModemProfile Generic => All[0];

    public static ModemProfile Get(string id) =>
        All.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)) ?? Generic;

    public static ModemProfile Match(string model)
    {
        var normalized = model.Trim().ToUpperInvariant();
        return All.Skip(1).FirstOrDefault(profile =>
            profile.ModelPrefixes.Any(prefix => normalized.StartsWith(prefix, StringComparison.Ordinal))) ?? Generic;
    }
}
