using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;

namespace Yeastar;

/// <summary>
/// Its a class for connecting to Yeastar TG800 device.
/// </summary>
public class TG800
{
    public record OnMessageReceivedEventArgs(GsmPort Port, GsmMessage Message);

    private const int ReadBufferSize = 1024;
    private const string EndOfMessage = "\r\n\r\n";
    private TcpClient? _tcpClient;
    private string _username, _password;
    private readonly YeastarResponseResolver _resolver;
    private CancellationTokenSource? _readingCancellationTokenSource;
    private readonly byte[] _readingBuffer = new byte[ReadBufferSize];
    private readonly Dictionary<int, List<GsmMessage>> _messages = new();
    public event EventHandler<OnMessageReceivedEventArgs>? OnMessageReceived;
    public event EventHandler? OnLoginFailed;

    public string IP { get; private set; }
    public int Port { get; private set; }
    public bool IsConnected { get; private set; }
    public List<GsmPort> Ports { get; } = [];

    public TG800()
    {
        _resolver = new YeastarResponseResolver();
        _resolver.UnresolvedEvent += (s, e) => Console.WriteLine($"Unresolved: {e}");
        _resolver.StatusEvent += (s, e) => UpdateStatus(e);
        _resolver.PortStatusEvent += (s, e) => UpdatePortStatus(e);
        _resolver.NewMessageEvent += (s, e) => NewMessageReceived(e);
        _resolver.Setup();
    }

    private void UpdateStatus(YeastarResult result)
    {
        Ports.Clear();
        Ports.AddRange(result.ExtractPorts());
        Console.WriteLine("Ports updated.");
    }

    private void UpdatePortStatus(YeastarResult result)
    {
        var channel = result.Find("D-channel");
        if (channel is null)
            return;

        if (!int.TryParse(channel.Value, out var channelNumber))
            return;

        var port = Ports.FirstOrDefault(p => p.DChannel.Equals(channelNumber));

        port?.Parse(result);
        Console.WriteLine($"Port {port?.Port ?? 0} updated.");
    }

    private void NewMessageReceived(YeastarResult result)
    {
        var msg = GsmMessage.Parse(result);
        var port = Ports.FirstOrDefault(p => p.Span.Equals(msg.Span));
        if (port is null)
            return;

        if (msg.ID <= 0)
        {
            OnMessageReceived?.Invoke(this, new OnMessageReceivedEventArgs(port, msg));
            return;
        }

        if (!_messages.ContainsKey(msg.ID))
            _messages.Add(msg.ID, []);

        if (!_messages.TryGetValue(msg.ID, out var list))
        {
            list = [];
            _messages.Add(msg.ID, list);
        }

        if (list.Exists(m => m.Index.Equals(msg.Index)))
            return;

        list.Add(msg);

        if (!msg.Total.Equals(list.Count))
            return;


        var message = list.OrderBy(m => m.Index)
            .Select(m => m.Message)
            .ToList();

        var concatMessages = string.Join("", message);

        var finalMessage = list.First() with { Message = concatMessages };

        OnMessageReceived?.Invoke(this, new OnMessageReceivedEventArgs(port, finalMessage));

        // Clear cache
        list.Clear();
        _messages.Remove(msg.ID);
    }

    public async Task ConnectAsync(string ip, int port, CancellationToken cancellationToken = default)
    {
        if (IsConnected)
            throw new Exception("Device is already connected. Disconnect the device and try again.");

        var ipAddress = IPAddress.Parse(ip);
        var ipEndPoint = new IPEndPoint(ipAddress, port);
        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(ipEndPoint, cancellationToken);
        IP = ip;
        Port = port;
        IsConnected = true;
    }

    public async Task StartAsync()
    {
        _readingCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _readingCancellationTokenSource.Token;
        _ = ListeningAsync(cancellationToken);
        await Task.Delay(200, cancellationToken);
    }

    public async Task LoginAsync(string userName, string password)
    {
        _username = userName;
        _password = password;

        var command = YeastarCommands.GenerateLoginCommand(userName, password);

        await WriteAsync(command);
        //var response = await ReadAsync();

        //response.IsAuthenticated();
    }

    public void Stop()
    {
        _readingCancellationTokenSource?.Cancel(true);
    }

    public Task GetStatusAsync()
        => WriteAsync(YeastarCommands.StatusCommand);

    public Task GetStatusAsync(int portNumber)
    {
        var command = YeastarCommands.GenerateSpanStatusCommand(portNumber + 1);
        return WriteAsync(command);
    }

    public async Task<string> SendSmsAsync(int portNumber, string destination, string message, string smsId)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query.Add("account", _username);
        query.Add("password", _password);
        query.Add("port", portNumber.ToString());
        query.Add("destination", destination);
        query.Add("content", message);
        var url = $"http://{IP}/cgi/WebCGI?1500101={query}";
        var uri = new Uri(url);

        Console.WriteLine(url);

        var result =
           await uri.SendAsync(HttpMethod.Get, HttpVersion.Version11, default);

        return result;
    }

    private async Task ListeningAsync(CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine("Start listening...");
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Waiting...");
                await ReadAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation cancelled.");
        }
        catch (IOException)
        {
            Console.WriteLine("Device is disconnected.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Listening error:\n{e.Message}");
        }
    }

    private async Task WriteAsync(string command)
    {
        var stream = _tcpClient?.GetStream()
                     ?? throw new InvalidOperationException("TCP client is not connected.");

        //Console.WriteLine(command);

        var memory = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(command));
        await stream.WriteAsync(memory);
        await Task.Delay(200);
    }

    private async Task ReadAsync(CancellationToken cancellationToken = default)
    {
        var stream = _tcpClient?.GetStream()
                     ?? throw new InvalidOperationException("TCP client is not connected.");
        string text;
        var response = new StringBuilder();

        do
        {
            var readCount = await stream.ReadAsync(_readingBuffer, 0, ReadBufferSize, cancellationToken);

            text = Encoding.UTF8.GetString(_readingBuffer, 0, readCount);

            response.Append(text);
            if (text.Equals("Response: Error\r\n", StringComparison.InvariantCultureIgnoreCase))
            {
                OnLoginFailed?.Invoke(this, default!);
                return;
            }
        } while (!text.EndsWith(EndOfMessage));

        Console.WriteLine($">>START>>\n{response}\n<<END<<");
        var result = response.ToString();
        response.Clear();
        _resolver.Resolve(result);
    }
}