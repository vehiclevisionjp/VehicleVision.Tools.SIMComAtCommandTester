using System.Text.RegularExpressions;

namespace VehicleVision.SimComAt;

public static partial class AtCommandRedactor
{
    public static string Redact(string command)
    {
        if (command.StartsWith("AT+CGAUTH=", StringComparison.OrdinalIgnoreCase))
            return CgAuthRegex().Replace(command, "$1\"***\",\"***\"");
        if (command.StartsWith("AT+CSTT=", StringComparison.OrdinalIgnoreCase))
            return CsttRegex().Replace(command, "$1\"***\",\"***\"");
        if (command.StartsWith("+CGAUTH:", StringComparison.OrdinalIgnoreCase))
            return CgAuthResponseRegex().Replace(command, "$1,\"***\"");
        if (command.StartsWith("AT+CMQTTCONNECT=", StringComparison.OrdinalIgnoreCase))
            return MqttConnectRegex().Replace(command, "$1,\"***\",\"***\"");
        if (command.StartsWith("AT+HTTPPARA=\"URL\"", StringComparison.OrdinalIgnoreCase))
            return "AT+HTTPPARA=\"URL\",\"***\"";
        return command;
    }

    [GeneratedRegex("^(AT\\+CGAUTH=\\d+,\\d+,)\"[^\"]*\",\"[^\"]*\"$", RegexOptions.IgnoreCase)]
    private static partial Regex CgAuthRegex();

    [GeneratedRegex("^(AT\\+CSTT=\"[^\"]*\",)\"[^\"]*\",\"[^\"]*\"$", RegexOptions.IgnoreCase)]
    private static partial Regex CsttRegex();

    [GeneratedRegex("^(\\+CGAUTH:\\s*\\d+,\\d+),.*$", RegexOptions.IgnoreCase)]
    private static partial Regex CgAuthResponseRegex();

    [GeneratedRegex("^(AT\\+CMQTTCONNECT=.*),\"[^\"]*\",\"[^\"]*\"$", RegexOptions.IgnoreCase)]
    private static partial Regex MqttConnectRegex();
}
