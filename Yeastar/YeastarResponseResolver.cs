namespace Yeastar;

public class YeastarResponseResolver
{
    public event EventHandler<YeastarResult>? UnresolvedEvent;
    public event EventHandler<YeastarResult>? StatusEvent;
    public event EventHandler<YeastarResult>? PortStatusEvent;
    public event EventHandler<YeastarResult>? NewMessageEvent;
    public event EventHandler<YeastarResult>? SmsDeliveryEvent;

    private readonly List<(Func<YeastarResult, bool> F, EventHandler<YeastarResult>? H)> _conditions = [];

    public void Setup()
    {
        _conditions.Clear();
        _conditions.Add((IsStatus, StatusEvent));
        _conditions.Add((IsPortStatus, PortStatusEvent));
        _conditions.Add((IsNewMessage, NewMessageEvent));
        _conditions.Add((IsSmsDelivery, SmsDeliveryEvent));
    }

    public void Resolve(string response)
    {
        var result = YeastarResult.Process(response);

        foreach (var condition in _conditions)
        {
            if (!condition.F.Invoke(result))
                continue;

            condition.H?.Invoke(this, result);
            return;
        }

        UnresolvedEvent?.Invoke(this, result);
    }

    private bool IsStatus(YeastarResult result)
    {
        return result.Exists("Response", "Follows") &&
               result.Exists("Privilege", "SMSCommand") &&
               result.Find("GSM span 2") is not null;
    }

    private bool IsPortStatus(YeastarResult result)
    {
        return result.Exists("Response", "Follows") &&
               result.Exists("Privilege", "SMSCommand") &&
               result.Find("D-channel") is not null;
    }

    private bool IsNewMessage(YeastarResult result)
    {
        return result.Exists("Event", "ReceivedSMS");
    }

    private bool IsSmsDelivery(YeastarResult result)
    {
        return result.Exists("Event", "UpdateSMSSend");
    }
}