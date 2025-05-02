using System.Diagnostics;
using System.Web;

namespace Yeastar;

[DebuggerDisplay("Id:{ID}, Sender:{Sender}, Message: {Message} ({Index} of {Total})")]
public record GsmMessage
{
    public int ID { get; init; }
    public int Span { get; init; }
    public int Index { get; init; }
    public int Total { get; init; }
    public string Sender { get; init; }
    public string Message { get; init; }
    public DateTime ReceiveTime { get; init; }

    public static GsmMessage Parse(YeastarResult result)
    {
        var mID = result.Find("ID")?.Value ?? "0";
        var mSpan = result.Find("GsmSpan")?.Value ?? "0";
        var mIndex = result.Find("Index")?.Value ?? "0";
        var mTotal = result.Find("Total")?.Value ?? "1";
        var mSender = result.Find("Sender")?.Value ?? string.Empty;
        var mMessage = HttpUtility.UrlDecode(result.Find("Content")?.Value ?? string.Empty);
        var mReceiveTime = result.Find("Recvtime")?.Value ?? string.Empty;
        var output = new GsmMessage
        {
            ID = int.Parse(mID == string.Empty ? "0" : mID),
            Span = int.Parse(mSpan),
            Index = int.Parse(mIndex),
            Total = int.Parse(mTotal),
            Sender = mSender,
            Message = mMessage,
            ReceiveTime = DateTime.Parse(mReceiveTime)
        };
        return output;
    }

}