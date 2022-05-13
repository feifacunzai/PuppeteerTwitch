namespace PuppeteerTwitch
{
    public class Config
    {
        private IConfiguration _configuration { get; set; }

        public Config(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string ChromePath => _configuration.GetValue<string>("chromePath");

        public string Token => _configuration.GetValue<string>("token");

        public string Channel => _configuration.GetValue<string>("channel");

        public string GqlSha256Hash => _configuration.GetValue<string>("gqlSha256Hash");

        public bool ListenChat = false;

        public string Linebreak => "=========================";
    }
}