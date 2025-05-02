using System.Net.Sockets;
using System.Text;

namespace Yeastar;

public static class TcpClientForHttp
{
    public static async Task<string> SendAsync(
        this Uri uri,
        HttpMethod httpMethod,
        Version httpVersion,
        CancellationToken cancellationToken)
    {
        var strHttpRequest = $"{httpMethod} {uri.PathAndQuery} HTTP/{httpVersion}\r\n";
        strHttpRequest += $"Host: {uri.Host}:{uri.Port}\r\n";
        // Any other HTTP headers can be added here ....
        strHttpRequest += "\r\n";

        var ms = await SendWebRequest(uri, strHttpRequest, cancellationToken);
        var result = Encoding.ASCII.GetString(ms.ToArray());
        return result;
    }

    private static async Task<MemoryStream> SendWebRequest(Uri uri, string request, CancellationToken cancellationToken)
    {
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(uri.Host, uri.Port, cancellationToken);
        await using var ns = tcpClient.GetStream();

        var resultStream = new MemoryStream();
        await using var nsWriter = new StreamWriter(ns);
        await nsWriter.WriteAsync(request);
        await nsWriter.FlushAsync(cancellationToken);
        await ns.CopyToAsync(resultStream, cancellationToken);
        resultStream.Position = 0;
        return resultStream;
    }

}