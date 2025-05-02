using System.Diagnostics;

namespace Yeastar;


[DebuggerDisplay("[{Port}] Span: {Span}, DChannel: {DChannel}, IMEI:{IMEI}, {Status}, Network: {NetworkName}-{NetworkStatus}, State: {State}")]
public class GsmPort
{
    public GsmPort(int port, string status = "Unknown")
    {
        Port = port;
        Span = port + 1;
        DChannel = port * 2;
        Status = status;
    }

    public int Port { get; private init; }
    public int Span { get; private init; }
    public int DChannel { get; private set; }
    public string Status { get; private set; }
    public string Type { get; private set; }
    public string Manufacturer { get; private set; }
    public string Model { get; private set; }
    public string IMEI { get; private set; }
    public string Revision { get; private set; }
    public string NetworkName { get; private set; }
    public string NetworkStatus { get; private set; }
    public string SignalQuality { get; private set; }
    public string SmsCenter { get; private set; }
    public string State { get; private set; }


    public void Parse(YeastarResult result)
    {
        if (int.TryParse(result.Find("D-Channel")?.Value, out var dChannel))
            DChannel = dChannel;

        IMEI = result.Find("Model IMEI")?.Value ?? IMEI;
        Type = result.Find("type")?.Value ?? Type;
        State = result.Find("State")?.Value ?? State;
        Model = result.Find("Model name")?.Value ?? Model;
        Status = result.Find("status")?.Value ?? Status;
        Revision = result.Find("Revision")?.Value ?? Revision;
        SmsCenter = result.Find("SIM SMS Center Number")?.Value ?? SmsCenter;
        NetworkName = result.Find("Network Name")?.Value ?? NetworkName;
        Manufacturer = result.Find("Manufacturer")?.Value ?? Manufacturer;
        NetworkStatus = result.Find("Network Status")?.Value ?? NetworkStatus;
        SignalQuality = result.Find("Signal Quality")?.Value ?? SignalQuality;
    }
}