namespace TVTComment.Model.NichanUtils
{
    /// <summary>
    /// <see cref="ChannelInfo"/>から対応する板URL・スレッドタイトルキーワードを得る
    /// </summary>
    class ThreadResolver
    {
        private readonly ChannelDatabase channelDatabase;
        private readonly BoardDatabase boardDatabase;
        
        public ThreadResolver(ChannelDatabase channelDatabase, BoardDatabase boardDatabase)
        {
            this.channelDatabase = channelDatabase;
            this.boardDatabase = boardDatabase;
        }

        public MatchingThread Resolve(ChannelInfo channelInfo)
        {
            ushort networkId = channelInfo.NetworkId, serviceId = channelInfo.ServiceId;
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
