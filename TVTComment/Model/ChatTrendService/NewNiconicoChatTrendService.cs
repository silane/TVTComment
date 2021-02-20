using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TVTComment.Model.NiconicoUtils;

namespace TVTComment.Model.ChatTrendService
{
    class NewNiconicoChatTrendService : IChatTrendService
    {
        public string Name => "新ニコニコ実況";
        public TimeSpan UpdateInterval => new TimeSpan(0, 0, 1, 0);
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly LiveIdResolver liveIdResolver;
        private readonly Dictionary<string, int[]> forces = new Dictionary<string, int[]>();

        public NewNiconicoChatTrendService(LiveIdResolver liveIdResolver)
        {
            this.liveIdResolver = liveIdResolver;
        }

        public void Dispose()
        {
            httpClient.CancelPendingRequests();
        }

        public async Task<IForceValueData> GetForceValueData()
        {
            var lives = liveIdResolver.GetLiveIdList().Where(x => x.Contains("ch")).Select(x => x.Replace("ch", "")).Distinct();
            try
            {
                foreach (var id in lives)
                {
                    var jk = await httpClient.GetStreamAsync(@$"https://public.api.nicovideo.jp/v1/channel/channelapp/channels/{id}/lives.json?sort=channelpage");
                    var obj = TrendJsonUtils.ToObject<NewNiconicoTrendJson>(jk);

                    var canAdded = !forces.ContainsKey(obj.Data[0].SocialGroupId);
                    if (canAdded)
                    {
                        forces.Add(obj.Data[0].SocialGroupId, new int[] { obj.Data[0].CommentCount, obj.Data[0].CommentCount });
                    }
                    else
                    {
                        forces[obj.Data[0].SocialGroupId] = new int[] { forces[obj.Data[0].SocialGroupId][1], obj.Data[0].CommentCount };
                    }
                    var random = new Random();
                    await Task.Delay(1000 + random.Next(0, 101));
                }
            }
            catch (HttpRequestException e)
            {
                throw new ChatTrendServiceException("勢い値データのタウンロードに失敗しました", e);
            }
            return new NewNiconicoForceValueData(forces, liveIdResolver);
        }
    }
}
