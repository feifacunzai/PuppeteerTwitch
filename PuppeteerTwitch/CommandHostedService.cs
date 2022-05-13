namespace PuppeteerTwitch
{
    public class CommandHostedService : IHostedService
    {
        private IHostApplicationLifetime _hostApplicationLifetime;
        private ILogger<CommandHostedService> _logger;
        private Config _config;
        private bool _stop { get; set; }
        private Task _backgroundTask = null!;
        private GQLService _gQLService = null!;

        public CommandHostedService(IHostApplicationLifetime hostApplicationLifetime, ILogger<CommandHostedService> logger, Config config, GQLService gQLService)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _logger = logger;
            _config = config;
            _gQLService = gQLService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("🎹 Command service starting...");
            this._backgroundTask = Task.Run(async () => { await this.ExecuteAsync(); }, cancellationToken);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine(_config.Linebreak);
            Console.WriteLine("🛑 Command service stopping...(Press <Enter> to continue)");
            this._stop = true;
            await this._backgroundTask;
            Console.WriteLine("🈲 Command service stopped!!");

        }

        private async Task ExecuteAsync()
        {
            Console.WriteLine(_config.Linebreak);
            Console.WriteLine("👌 Command is ready!!");

            while (!this._stop)
            {
                var command = String.Empty;
                ConsoleKeyInfo keyInfo;
                Console.Write("> ");
                do
                {
                    keyInfo = Console.ReadKey(true);
                    if (keyInfo.Key == ConsoleKey.Backspace)
                    {
                        if (command.Length >= 1)
                        {
                            int cursorCol = Console.CursorLeft - 1;
                            int oldLength = command.Length;
                            int extraRows = oldLength / 80;
                            command = command.Substring(0, oldLength - 1);
                            Console.CursorLeft = 0;
                            Console.CursorTop = Console.CursorTop - extraRows;
                            Console.Write("> " + command + new String(' ', oldLength - command.Length));
                            Console.CursorLeft = cursorCol;
                        }
                        continue;
                    }
                    Console.Write(keyInfo.KeyChar);
                    if (keyInfo.Key != ConsoleKey.Enter)
                        command += keyInfo.KeyChar;
                } while (keyInfo.Key != ConsoleKey.Enter);

                Console.WriteLine();

                switch (command.ToLower())
                {
                    case "exit":
                        Console.WriteLine(_config.Linebreak);
                        _hostApplicationLifetime.StopApplication();
                        break;
                    case "chat":
                        Console.WriteLine(_config.Linebreak);
                        _config.ListenChat = !_config.ListenChat;
                        ChatSwitch.Invoke(this, _config.ListenChat);
                        break;
                    case "point":
                        Console.WriteLine(_config.Linebreak);
                        var pointContext = await _gQLService.ChannelPointsContextAsync();
                        Console.WriteLine($"Point: {pointContext.data.community.channel.self.communityPoints.balance}");
                        break;
                    case "help":
                        Console.WriteLine(_config.Linebreak);
                        Console.WriteLine("chat: Show messages");
                        Console.WriteLine("point: Show points");
                        Console.WriteLine("exit: Stop application");
                        break;
                }
            }
        }

        public EventHandler<bool> ChatSwitch = null!;


    }
}