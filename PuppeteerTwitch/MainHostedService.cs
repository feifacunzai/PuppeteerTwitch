using OutputColorizer;
using PuppeteerSharp;
using Serilog;

namespace PuppeteerTwitch
{
    public class MainHostedService : IHostedService
    {
        private bool _stop;
        private Task _backgroundTask = null!;
        private Config _config;
        private Browser _browser = null!;
        private Page _page = null!;
        private CDPSession _cdp = null!;
        public CancellationToken _cancellationToken = new CancellationToken();
        private IHostApplicationLifetime _hostApplicationLifetime;
        private ILogger<MainHostedService> _logger;
        private GQLService _gQLService;

        public MainHostedService(IHostApplicationLifetime hostApplicationLifetime, ILogger<MainHostedService> logger, Config config, GQLService gQLService)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _config = config;
            _logger = logger;
            _gQLService = gQLService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("📺 Main service starting...");
            try
            {
                // check config
                Console.Write("🔎 Checking config file...");
                CheckConfig();
                Console.WriteLine("OK");

                // launch browser
                Console.Write("🎬 Launching browser...");
                await LaunchBrowserAsync();
                Console.WriteLine("OK");

                // goto watch
                Console.Write($"👀 Goto watch {_config.Channel}...");
                await CreatePage();
                await GotoWatch();
                Console.WriteLine("OK");

                // check login
                Console.Write("🔑 Check login...");
                await CheckLogin();
                Console.WriteLine("OK");
            }
            catch (TargetClosedException ex)
            {
                Console.WriteLine($"\r\n⛔ Error [{ex.Message}]");
                _hostApplicationLifetime.StopApplication();
            }
            catch (PuppeteerException ex)
            {
                Console.WriteLine($"\r\n⛔ Error [{ex.Message}]");
                _hostApplicationLifetime.StopApplication();
            }
            catch (KeyNotFoundException ex)
            {
                Console.WriteLine($"\r\n⛔ Error [{ex.Message}]");
                _hostApplicationLifetime.StopApplication();

            }

            this._backgroundTask = Task.Run(async () => { await this.ExecuteAsync(cancellationToken); }, cancellationToken);

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine(_config.Linebreak);
            Console.WriteLine("🛑 Main service stopping...");
            if (_browser != null && !_browser.IsClosed)
            {
                await _browser.CloseAsync();
            }
            this._stop = true;
            await this._backgroundTask;
            Console.WriteLine("🈲 Main service stopped!!");
        }

        private async Task ExecuteAsync(CancellationToken cancel)
        {
            while (!this._stop)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancel);
            }
        }

        private void CheckConfig()
        {
            if (string.IsNullOrEmpty(_config.ChromePath))
            {
                throw new KeyNotFoundException("Config no chrome path");
            }
            if (string.IsNullOrEmpty(_config.Token))
            {
                throw new KeyNotFoundException("Config no token");
            }
            if (string.IsNullOrEmpty(_config.Channel))
            {
                throw new KeyNotFoundException("Config no channel");
            }
        }

        private async Task LaunchBrowserAsync()
        {
            if (!_stop)
            {
                var browserLogger = LoggerFactory.Create(builder =>
                {
                    builder.AddSerilog();
                });
                _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = false,
                    ExecutablePath = _config.ChromePath,
                    Args = new string[] {
                    "--disable-dev-shm-usage",
                    "--disable-accelerated-2d-canvas",
                    "--no-first-run",
                    "--no-zygote",
                    "--disable-gpu",
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                }
                }, browserLogger);
                _browser.Disconnected += async (object? sender, EventArgs e) =>
                {
                    if (!_stop)
                    {
                        await StopAsync(_cancellationToken);
                    }
                };
            }
        }

        private async Task CreatePage()
        {
            if (!_stop && _browser != null && _browser.IsConnected)
            {
                _page = await _browser.NewPageAsync();
            }
        }
        private async Task GotoWatch()
        {
            if (!_stop && _browser != null && _page != null && _browser.IsConnected)
            {
                await _page.SetCookieAsync(new CookieParam()
                {
                    Domain = ".twitch.tv",
                    Path = "/",
                    SameSite = SameSite.None,
                    Secure = true,
                    Session = false,
                    Name = "auth-token",
                    Value = _config.Token
                });
                await _page.SetUserAgentAsync("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36");
                await _page.GoToAsync(
                    $"https://twitch.tv/{_config.Channel}");
                _cdp = await _page.Target.CreateCDPSessionAsync();
                _cdp.MessageReceived += (e, q) =>
                {
                    if (q.MessageID == "Network.webSocketFrameReceived")
                    {
                        var messageData = q.MessageData.ToObject<WebSocketMessageData>();
                        var messageParse = new ChatMessageModel(messageData.response.payloadData);
                        if (!string.IsNullOrEmpty(messageParse.DisplayName))
                        {
                            Console.WriteLine($"\t{messageParse.DisplayName}({messageParse.UserId}): {messageParse.Content}".Replace("\r\n", ""));
                            Log.Information("{displayName}({userId): {message}", messageParse.DisplayName, messageParse.UserId, messageParse.Content);
                        }
                    }
                };
                _page.Console += async (e, q) =>
                {
                    var consoleType = q.Message.Type.ToString().ToLower();
                    if (consoleType == "info")
                    {
                        var point = await _gQLService.ChannelPointsContextAsync();
                        var balance = point.data.community.channel.self.communityPoints.balance;
                        Colorizer.WriteLine($"[{ConsoleColor.Yellow}!{q.Message.Text}] ({balance})");
                    }
                };
                await _page.EvaluateExpressionAsync(@"
                    localStorage.setItem('mature', 'true');
                    localStorage.setItem('video-muted', '{""default"":true}');
                    localStorage.setItem('video-quality', '{""default"":""160p30""}');
                    function claimPoint() {
                        var button = document.querySelector('.community-points-summary div:last-child button');
                        if(button) {
                            console.info('Get community points');
                            button.click();
                        }
                        setTimeout(function() {
                            claimPoint();
                        }, 6000);
                    }
                    claimPoint();
                ");
                await _page.SetViewportAsync(new ViewPortOptions()
                {
                    Width = 1024,
                    Height = 512
                });
            }
        }
        private async Task CheckLogin()
        {
            if (!_stop && _page != null)
            {
                await _page.GetCookiesAsync();
            }
            else
            {
                throw new NullReferenceException("Page is null");
            }
        }

        public async Task ChatOpenAsync()
        {
            Console.WriteLine("Chat watcher on");
            await _cdp.SendAsync("Network.enable");
            await _cdp.SendAsync("Page.enable");
        }

        public async Task ChatCloseAsync()
        {
            Console.WriteLine("Chat watcher off");
            await _cdp.SendAsync("Network.disable");
            await _cdp.SendAsync("Page.disable");
        }
    }
}