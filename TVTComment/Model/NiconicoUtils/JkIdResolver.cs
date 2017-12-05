using System.Collections.Generic;

namespace TVTComment.Model.NiconicoUtils
{
    /// <summary>
    /// ネットワークID・サービスIDとニコニコ実況IDの対応付けを行う
    /// </summary>
    class JkIdResolver
    {
        private ChannelDatabase channelDatabase;
        private JkIdTable jkIdTable;

        public JkIdResolver(ChannelDatabase channelDatabase,JkIdTable jkIdTable)
        {
            this.channelDatabase = channelDatabase;
            this.jkIdTable = jkIdTable;
        }

        /// <summary>
        /// 対応する実況IDを探す 対応がなければ0を返す
        /// </summary>
        /// <param name="networkId">ネットワークID 不明なら0</param>
        /// <param name="serviceId">サービスID</param>
        public int Resolve(ushort networkId,ushort serviceId)
        {
            if (networkId == 0)
            {
                //録画ファイルではネットワークIDが分からないのでサービスIDだけで検索
                //ニコニコ実況に対応しているチャンネルで同じサービスIDのものはないはずなので普通はこれで大丈夫
                foreach (ChannelEntry channel in channelDatabase.GetByServiceId(serviceId))
                {
                    int jkid = jkIdTable.GetJkId(channel);
                    if (jkid != 0)
                        return jkid;
                }
                return 0;
            }
            else
            {
                ChannelEntry channel = channelDatabase.GetByNetworkIdAndServiceId(networkId, serviceId);
                if (channel == null)
                    return 0;
                return jkIdTable.GetJkId(channel);
            }
        }
    }
}
