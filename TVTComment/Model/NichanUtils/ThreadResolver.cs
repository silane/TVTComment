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
            if (networkId != 0)
            {
                ChannelEntry channel = channelDatabase.GetByNetworkIdAndServiceId(networkId, serviceId);//channels.txtの登録チャンネルに解決
                if (channel == null)
                    return null;
                return boardDatabase.GetMatchingThreadForChannel(channel);
            }
            else
                return null;
        }
    }
}
