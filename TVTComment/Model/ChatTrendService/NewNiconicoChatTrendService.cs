using System;
using System.Collections.Generic;
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
        private NiconicoUtils.LiveIdResolver liveIdResolver;
        private Dictionary<string, int[]> forces = new Dictionary<string, int[]>();

        public NewNiconicoChatTrendService(NiconicoUtils.LiveIdResolver liveIdResolver)
        {
            this.liveIdResolver = liveIdResolver;
        }

        public void Dispose()
        {
            httpClient.CancelPendingRequests();
        }

        public async Task<IForceValueData> GetForceValueData()
        {
            try
            {
                var jk1 = await httpClient.GetStreamAsync(@"https://public.api.nicovideo.jp/v1/channel/channelapp/channels/2646436/lives.json?sort=channelpage");
                var jk2 = await httpClient.GetStreamAsync(@"https://public.api.nicovideo.jp/v1/channel/channelapp/channels/2646437/lives.json?sort=channelpage");
                var jk4 = await httpClient.GetStreamAsync(@"https://public.api.nicovideo.jp/v1/channel/channelapp/channels/2646438/lives.json?sort=channelpage");
                var jk5 = await httpClient.GetStreamAsync(@"https://public.api.nicovideo.jp/v1/channel/channelapp/channels/2646439/lives.json?sort=channelpage");
                var jk6 = await httpClient.GetStreamAsync(@"https://public.api.nicovideo.jp/v1/channel/channelapp/channels/2646440/lives.json?sort=channelpage");
                var jk7 = await httpClient.GetStreamAsync(@"https://public.api.nicovideo.jp/v1/channel/channelapp/channels/2646441/lives.json?sort=channelpage");
                var jk8 = await httpClient.GetStreamAsync(@"https://public.api.nicovideo.jp/v1/channel/channelapp/channels/2646442/lives.json?sort=channelpage");
                var jk9 = await httpClient.GetStreamAsync(@"https://public.api.nicovideo.jp/v1/channel/channelapp/channels/2646485/lives.json?sort=channelpage");
                var jk211 = await httpClient.GetStreamAsync(@"https://public.api.nicovideo.jp/v1/channel/channelapp/channels/2646846/lives.json?sort=channelpage");
                
                var jk1Obj = TrendJsonUtils.ToObject<NewNiconicoTrendJson>(jk1);
                var jk2Obj = TrendJsonUtils.ToObject<NewNiconicoTrendJson>(jk2);
                var jk4Obj = TrendJsonUtils.ToObject<NewNiconicoTrendJson>(jk4);
                var jk5Obj = TrendJsonUtils.ToObject<NewNiconicoTrendJson>(jk5);
                var jk6Obj = TrendJsonUtils.ToObject<NewNiconicoTrendJson>(jk6);
                var jk7Obj = TrendJsonUtils.ToObject<NewNiconicoTrendJson>(jk7);
                var jk8Obj = TrendJsonUtils.ToObject<NewNiconicoTrendJson>(jk8);
                var jk9Obj = TrendJsonUtils.ToObject<NewNiconicoTrendJson>(jk9);
                var jk211Obj = TrendJsonUtils.ToObject<NewNiconicoTrendJson>(jk211);

                if (forces.Count <= 0)
                {
                    forces.Add(jk1Obj.data[0].socialGroupId, new int[] { 0, jk1Obj.data[0].commentCount });
                    forces.Add(jk2Obj.data[0].socialGroupId, new int[] { 0, jk2Obj.data[0].commentCount });
                    forces.Add(jk4Obj.data[0].socialGroupId, new int[] { 0, jk4Obj.data[0].commentCount });
                    forces.Add(jk5Obj.data[0].socialGroupId, new int[] { 0, jk5Obj.data[0].commentCount });
                    forces.Add(jk6Obj.data[0].socialGroupId, new int[] { 0, jk6Obj.data[0].commentCount });
                    forces.Add(jk7Obj.data[0].socialGroupId, new int[] { 0, jk7Obj.data[0].commentCount });
                    forces.Add(jk8Obj.data[0].socialGroupId, new int[] { 0, jk8Obj.data[0].commentCount });
                    forces.Add(jk9Obj.data[0].socialGroupId, new int[] { 0, jk9Obj.data[0].commentCount });
                    forces.Add(jk211Obj.data[0].socialGroupId, new int[] { 0, jk211Obj.data[0].commentCount });
                }
                else
                {
                    forces[jk1Obj.data[0].socialGroupId] = new int[] { forces[jk1Obj.data[0].socialGroupId][1], jk1Obj.data[0].commentCount };
                    forces[jk2Obj.data[0].socialGroupId] = new int[] { forces[jk2Obj.data[0].socialGroupId][1], jk2Obj.data[0].commentCount };
                    forces[jk4Obj.data[0].socialGroupId] = new int[] { forces[jk4Obj.data[0].socialGroupId][1], jk4Obj.data[0].commentCount };
                    forces[jk5Obj.data[0].socialGroupId] = new int[] { forces[jk5Obj.data[0].socialGroupId][1], jk5Obj.data[0].commentCount };
                    forces[jk6Obj.data[0].socialGroupId] = new int[] { forces[jk6Obj.data[0].socialGroupId][1], jk6Obj.data[0].commentCount };
                    forces[jk7Obj.data[0].socialGroupId] = new int[] { forces[jk7Obj.data[0].socialGroupId][1], jk7Obj.data[0].commentCount };
                    forces[jk8Obj.data[0].socialGroupId] = new int[] { forces[jk8Obj.data[0].socialGroupId][1], jk8Obj.data[0].commentCount };
                    forces[jk9Obj.data[0].socialGroupId] = new int[] { forces[jk9Obj.data[0].socialGroupId][1], jk9Obj.data[0].commentCount };
                    forces[jk211Obj.data[0].socialGroupId] = new int[] { forces[jk211Obj.data[0].socialGroupId][1], jk211Obj.data[0].commentCount };
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
