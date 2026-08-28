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
        return command;
    }

    [GeneratedRegex("^(AT\\+CGAUTH=\\d+,\\d+,)\"[^\"]*\",\"[^\"]*\"$", RegexOptions.IgnoreCase)]
    private static partial Regex CgAuthRegex();

    [GeneratedRegex("^(AT\\+CSTT=\"[^\"]*\",)\"[^\"]*\",\"[^\"]*\"$", RegexOptions.IgnoreCase)]
    private static partial Regex CsttRegex();

    [GeneratedRegex("^(\\+CGAUTH:\\s*\\d+,\\d+),.*$", RegexOptions.IgnoreCase)]
    private static partial Regex CgAuthResponseRegex();
}
