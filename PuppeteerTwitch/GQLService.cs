using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace PuppeteerTwitch
{
    public class GQLService
    {
        private Config _config { get; set; }

        public GQLService(Config _config)
        {
            this._config = _config;
        }

        public async Task<ChannelPointsContextModel> ChannelPointsContextAsync()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", $"OAuth {_config.Token}");
                var data = new
                {
                    operationName = "ChannelPointsContext",
                    variables = new { channelLogin = _config.Channel },
                    extensions = new { persistedQuery = new { version = 1, sha256Hash = _config.GqlSha256Hash } }
                };
                var response = await client.PostAsJsonAsync("https://gql.twitch.tv/gql", data);
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonConvert.DeserializeObject<ChannelPointsContextModel>(responseContent);
                return responseData;
            }
        }
    }
}