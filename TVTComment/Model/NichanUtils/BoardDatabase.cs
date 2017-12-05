using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.NichanUtils
{
    /// <summary>
    /// 2chの板のリストとスレッド選択ルールのデータベース
    /// </summary>
    class BoardDatabase
    {
        private ThreadMapping threadMapping;
        public IEnumerable<BoardEntry> BoardList { get; }

        public BoardDatabase(IEnumerable<BoardEntry> boards,IEnumerable<ThreadMappingRuleEntry> threadMappingRuleEntries)
        {
            this.BoardList = boards;
            this.threadMapping = new ThreadMapping(threadMappingRuleEntries);
        }

        /// <summary>
        /// 板IDから板を検索 見つからなければnull
        /// </summary>
        public BoardEntry GetBoardById(string id)
        {
            return BoardList.FirstOrDefault(x => x.Id == id);
        }

        /// <summary>
        /// <see cref="ChannelEntry"/>に対応する板とスレッド名キーワードを返す 見つからなければnull
        /// </summary>
        public MatchingThread GetMatchingThreadForChannel(ChannelEntry channel)
        {
            string[] threadTitleKeywords;

            var boardAndThread = threadMapping.Get(channel);//板名とスレッドタイトルを得る
            if (boardAndThread == null)
                return null;
            BoardEntry board = this.GetBoardById(boardAndThread.BoardId);//板名から板URLと主要スレッド名を得る
            if (board == null)
                return null;
            threadTitleKeywords = boardAndThread.ThreadTitleKeywords ?? board.MainThreadTitleKeywords;
            if (threadTitleKeywords == null)
                return null;

            return new MatchingThread(board.Title, board.Uri, threadTitleKeywords);
        }
    }
}
