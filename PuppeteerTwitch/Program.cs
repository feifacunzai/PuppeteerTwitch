using PuppeteerTwitch;
using Serilog;

//Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.CancelKeyPress += (sender, e) =>
{
    Console.WriteLine();
};

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Host.ConfigureServices((hostContext, services) =>
{
    services.AddSingleton<Config>(new Config(hostContext.Configuration));
    services.AddSingleton<MainHostedService>();
    services.AddSingleton<CommandHostedService>();
    services.AddSingleton<GQLService>();
    services.AddHostedService<MainHostedService>(s => s.GetRequiredService<MainHostedService>());
    services.AddHostedService<CommandHostedService>(s => s.GetRequiredService<CommandHostedService>());
});

var app = builder.Build();

var mainHS = app.Services.GetService<MainHostedService>();
var commandHS = app.Services.GetService<CommandHostedService>();
commandHS!.ChatSwitch += async (sender, status) =>
{
    if (status)
    {
        await mainHS!.ChatOpenAsync();
    }
    else
    {
        await mainHS!.ChatCloseAsync();
    }
};

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"⛔ {ex.Message}");
    Log.Fatal(ex.Message);
}
finally
{
    Console.WriteLine("DONE");
    Log.CloseAndFlush();
}