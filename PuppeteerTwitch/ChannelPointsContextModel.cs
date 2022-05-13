#nullable disable

namespace PuppeteerTwitch
{
    public class ChannelPointsContextModel
    {
        public Data data { get; set; }

        public class Data
        {
            public Community community { get; set; }

            public class Community
            {
                public Channel channel { get; set; }

                public class Channel
                {
                    public Self self { get; set; }

                    public class Self
                    {
                        public CommunityPoints communityPoints { get; set; }

                        public class CommunityPoints
                        {
                            public int balance { get; set; }
                        }
                    }
                }
            }
        }
    }
}