using TVTComment.Model.TwitterUtils;

namespace TVTComment.Model.NiconicoUtils
{
    /// <summary>
    /// ネットワークID・サービスIDと検索ワードの対応付けを行う
    /// </summary>
    class SearchWordResolver
    {
        private ChannelDatabase channelDatabase;
        private SearchWordTable searchWordTable;

        public SearchWordResolver(ChannelDatabase channelDatabase, SearchWordTable searchWordTable)
        {
            this.channelDatabase = channelDatabase;
            this.searchWordTable = searchWordTable;
        }

        /// <summary>
        /// 対応する検索ワードを探す 対応がなければ空文字列を返す
        /// </summary>
        /// <param name="networkId">ネットワークID 不明なら0</param>
        /// <param name="serviceId">サービスID</param>
        public string Resolve(ushort networkId,ushort serviceId)
        {
            if (networkId == 0)
            {
                foreach (ChannelEntry channel in channelDatabase.GetByServiceId(serviceId))
                {
                    string serchWord = searchWordTable.GetSerchWord(channel);
                    if (serchWord.Equals(""))
                        return serchWord;
                }
                return "";
            }
            else
            {
                ChannelEntry channel = channelDatabase.GetByNetworkIdAndServiceId(networkId, serviceId);
                if (channel == null)
                    return "";
                return searchWordTable.GetSerchWord(channel);
            }
        }
    }
}
