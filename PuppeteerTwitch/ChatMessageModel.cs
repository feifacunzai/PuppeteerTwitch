namespace PuppeteerTwitch
{
    public class ChatMessageModel
    {
        public string? UserId { get; set; }
        public string? DisplayName { get; set; }
        public string? Content;

        public ChatMessageModel(string payloadData)
        {
            var splits = payloadData.Split(";");
            DisplayName = splits.FirstOrDefault(s => s.StartsWith("display-name"))?.Split("=")[1];
            UserId = splits.FirstOrDefault(s => s.StartsWith("user-id"))?.Split("=")[1] ?? DisplayName;
            Content = splits.FirstOrDefault(s => s.StartsWith("user-type"))?.Split(":")?.LastOrDefault();
        }
    }

    public class WebSocketMessageData
    {
        public string requestId { get; set; } = null!;

        public decimal timestamp { get; set; }

        public WebSocketResponse response { get; set; } = null!;
    }

    public class WebSocketResponse
    {
        public int opcode { get; set; }

        public bool masl { get; set; }

        public string payloadData { get; set; } = null!;
    }
}