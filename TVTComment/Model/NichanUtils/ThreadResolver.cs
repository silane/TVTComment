using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.NichanUtils
{
    /// <summary>
    /// ネットワークID・サービスIDと板URL・スレッドタイトルの対応付けを行う
    /// </summary>
    class ThreadResolver
    {
        ChannelDatabase channelDatabase;
        BoardDatabase boardDatabase;
        
        public ThreadResolver(ChannelDatabase channelDatabase,BoardDatabase boardDatabase)
        {
            this.channelDatabase = channelDatabase;
            this.boardDatabase = boardDatabase;
        }

        public MatchingThread Resolve(ushort networkId, ushort serviceId)
        {
            if (networkId == 0)
            {
                //録画ファイルなどではネットワークIDが分からないのでサービスIDだけで検索
                //BSとCSの間ではサービスIDが重複する可能性があるがほとんどないので割り切る
                foreach (ChannelEntry channel in channelDatabase.GetByServiceId(serviceId))
                {
                    MatchingThread ret = boardDatabase.GetMatchingThreadForChannel(channel);
                    if (ret != null)
                        return ret;
                }
                return null;
            }
            else
            {
                ChannelEntry channel = channelDatabase.GetByNetworkIdAndServiceId(networkId, serviceId);//channels.txtの登録チャンネルに解決
                if (channel == null)
                    return null;
                return boardDatabase.GetMatchingThreadForChannel(channel);
            }
        }
    }
}
