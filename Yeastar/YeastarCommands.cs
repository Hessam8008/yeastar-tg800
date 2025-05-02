namespace Yeastar;

internal static class YeastarCommands
{
    internal const string LoginCommand = "Action: Login\r\nUsername: {0}\r\nSecret: {1}\r\n\r\n";
    internal const string StatusCommand = "Action: smscommand\r\ncommand: gsm show spans\r\n\r\n";
    internal const string CheckPort = "Action: smscommand\r\ncommand: gsm show span {0}\r\n\r\n";

    public static string GenerateLoginCommand(string username, string password)
        => string.Format(LoginCommand, username, password);

    public static string GenerateSpanStatusCommand(int span)
        => string.Format(CheckPort, span);

    public static void IsAuthenticated(this string response)
    {
        var result = YeastarResult.Process(response);
        var isSuccess = result.Find("Response")?.Is("Success");
        if (isSuccess is null || !isSuccess.Value)
            throw new Exception(result.Find("Message")?.Value ?? "Authentication failed.");
    }

    public static List<GsmPort> ExtractPorts(this YeastarResult response)
    {
        var output = new List<GsmPort>();
        for (var i = 1; i <= 8; i++)
        {
            var gsm = $"GSM span {i + 1}";
            var value = response.Find(gsm)?.Value;
            if (string.IsNullOrEmpty(value))
                continue;

            var port = new GsmPort(i, value);

            output.Add(port);
        }

        return output;
    }

}