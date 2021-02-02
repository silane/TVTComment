using System.Collections.Generic;

namespace TVTComment.Model.NiconicoUtils
{
    /// <summary>
    /// ネットワークID・サービスIDとニコニコ生放送IDの対応付けを行う
    /// </summary>
    class LiveIdResolver
    {
        private readonly ChannelDatabase channelDatabase;
        private readonly LiveIdTable liveIdTable;

        public LiveIdResolver(ChannelDatabase channelDatabase, LiveIdTable liveIdTable)
        {
            this.channelDatabase = channelDatabase;
            this.liveIdTable = liveIdTable;
        }

        /// <summary>
        /// 対応する生放送IDを探す 対応がなければ空文字を返す
        /// </summary>
        /// <param name="networkId">ネットワークID 不明なら0</param>
        /// <param name="serviceId">サービスID</param>
        public string Resolve(ushort networkId, ushort serviceId)
        {
            if (networkId == 0)
            {
                //録画ファイルではネットワークIDが分からないのでサービスIDだけで検索
                //ニコニコ実況に対応しているチャンネルで同じサービスIDのものはないはずなので普通はこれで大丈夫
                foreach (ChannelEntry channel in channelDatabase.GetByServiceId(serviceId))
                {
                    string liveId = liveIdTable.GetLiveId(channel);
                    if (liveId != "")
                        return liveId;
                }
                return "";
            }
            else
            {
                ChannelEntry channel = channelDatabase.GetByNetworkIdAndServiceId(networkId, serviceId);
                if (channel == null)
                    return "";
                return liveIdTable.GetLiveId(channel);
            }
        }

        /// <summary>
        /// 生放送IDの一覧を返す
        /// </summary>
        public IEnumerable<string> GetLiveIdList()
        {
            return liveIdTable.GetLiveIdList();
        }
    }
}
