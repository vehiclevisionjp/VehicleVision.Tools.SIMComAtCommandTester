using System.IO.Ports;

namespace VehicleVision.SimComAt;

public static class SerialPortDiscovery
{
    public static IReadOnlyList<string> GetPortNames() =>
        SerialPort.GetPortNames().OrderBy(ParsePortNumber).ThenBy(x => x).ToArray();

    private static int ParsePortNumber(string value) =>
        int.TryParse(value.AsSpan(3), out var number) ? number : int.MaxValue;
}
