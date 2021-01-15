using System.Collections.Generic;
using System.Linq;

namespace TVTComment.Model.NichanUtils
{
    /// <summary>
    /// 2chの板のリストとスレッド選択ルールのデータベース
    /// </summary>
    class BoardDatabase
    {
        public IEnumerable<BoardEntry> BoardList { get; }
        public IEnumerable<ThreadMappingRuleEntry> ThreadMappingRuleEntries { get; }

        public BoardDatabase(IEnumerable<BoardEntry> boardList, IEnumerable<ThreadMappingRuleEntry> threadMappingRuleEntries)
        {
            this.BoardList = boardList;
            this.ThreadMappingRuleEntries = threadMappingRuleEntries;
        }

        /// <summary>
        /// <paramref name="channel"/>に対応する<see cref="ThreadMappingRuleEntry"/>を返す
        /// 見つからなければ<c>null</c>
        /// </summary>
        public ThreadMappingRuleEntry GetMatchingThreadMappingRuleEntry(ChannelEntry channel)
        {
            ThreadMappingRuleEntry ret = null;
            foreach (ThreadMappingRuleEntry entry in this.ThreadMappingRuleEntries)
            {
                switch (entry.Target)
                {
                    case ThreadMappingRuleTarget.Flags:
                        if (((ChannelFlags)entry.Value & channel.Flags) != 0)
                        {
                            ret = entry;
                        }
                        break;
                    case ThreadMappingRuleTarget.NSId:
                        if (entry.Value == (channel.NetworkId << 16 | channel.ServiceId))
                        {
                            ret = entry;
                        }
                        break;
                    case ThreadMappingRuleTarget.NId:
                        if (entry.Value == channel.NetworkId)
                        {
                            ret = entry;
                        }
                        break;
                }
            }
            return ret;
        }

        /// <summary>
        /// 板IDから板を検索 見つからなければ<c>null</c>
        /// </summary>
        public BoardEntry GetBoardEntryById(string id)
        {
            return this.BoardList.FirstOrDefault(x => x.Id == id);
        }

        /// <summary>
        /// <paramref name="channel"/>に対応する板とスレッド名キーワードを返す
        /// 見つからなければ<c>null</c>
        /// </summary>
        public MatchingThread GetMatchingThread(ChannelEntry channel)
        {
            string[] threadTitleKeywords;

            var boardAndThread = this.GetMatchingThreadMappingRuleEntry(channel);//板名とスレッドタイトルを得る
            if (boardAndThread == null)
                return null;
            BoardEntry board = this.GetBoardEntryById(boardAndThread.BoardId);//板名から板URLと主要スレッド名を得る
            if (board == null)
                return null;
            threadTitleKeywords = boardAndThread.ThreadTitleKeywords ?? board.MainThreadTitleKeywords;
            if (threadTitleKeywords == null)
                return null;

            return new MatchingThread(board.Title, board.Uri, threadTitleKeywords);
        }
    }
}
