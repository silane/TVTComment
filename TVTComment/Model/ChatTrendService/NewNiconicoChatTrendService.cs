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
        private static HttpClient httpClient = new HttpClient();
        private LiveIdResolver liveIdResolver;
        private Dictionary<string, int[]> forces = new Dictionary<string, int[]>();

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
            var lives = liveIdResolver.GetLiveIdList().Where(x => x.Contains("ch")).Select(x => x.Replace("ch","")).Distinct();
            try
            {
                foreach(var id in lives)
                {
                    var jk = await httpClient.GetStreamAsync(@$"https://public.api.nicovideo.jp/v1/channel/channelapp/channels/{id}/lives.json?sort=channelpage");
                    var obj = TrendJsonUtils.ToObject<NewNiconicoTrendJson>(jk);

                    var canAdded = !forces.ContainsKey(obj.data[0].socialGroupId);
                    if (canAdded)
                    {
                        forces.Add(obj.data[0].socialGroupId, new int[] { obj.data[0].commentCount, obj.data[0].commentCount });
                    }
                    else
                    {
                        forces[obj.data[0].socialGroupId] = new int[] { forces[obj.data[0].socialGroupId][1], obj.data[0].commentCount };
                    }
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
