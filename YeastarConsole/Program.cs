// See https://aka.ms/new-console-template for more information

using Yeastar;

Console.WriteLine("Hello, World!");


var tg = new TG800();
tg.OnLoginFailed += Tg_OnLoginFailed;
tg.OnMessageReceived += Tg_OnMessageReceived;

void Tg_OnLoginFailed(object? sender, EventArgs e)
{
    Console.WriteLine("# Login failed #");
}

void Tg_OnMessageReceived(object? sender, TG800.OnMessageReceivedEventArgs args)
{
    Console.WriteLine("[NEW] {0} | {1}", args.Port.Port, args.Message.Message);
}

try
{
    await tg.ConnectAsync("192.168.5.150", 5038);
    await tg.StartAsync();

    await tg.LoginAsync("user1", "123456");

    await tg.GetStatusAsync();

    foreach (var port in tg.Ports)
        await tg.GetStatusAsync(port.Port);

}
catch (Exception e)
{
    Console.WriteLine(e);
    return;
}

tg.Ports.ForEach(x =>
{
    Console.WriteLine($"P: {x.Port}, S: {x.Span}, C: {x.DChannel}, Status: {x.Status}, {x.State}");
});



Console.WriteLine("Press Esc to exit...");

while (true)
{
    var key = Console.ReadKey();
    if (key.Key == ConsoleKey.Escape)
    {
        break;
    }
    else if (key.Key == ConsoleKey.S)
    {
        Console.WriteLine("Press any key to stop listening...");
        var result = await tg.SendSmsAsync(1, "09301185650", "تست ارسال پیامک", "");
        Console.WriteLine(result);
    }

}

tg.Stop();

Console.WriteLine("Press any key to close...");
Console.ReadKey();