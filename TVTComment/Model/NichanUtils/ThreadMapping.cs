using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.NichanUtils
{
    /// <summary>
    /// <see cref="ThreadMappingRuleEntry"/>に基づいた<see cref="ChannelEntry"/>と2chの板・スレッドの対応を表す
    /// </summary>
    class ThreadMapping
    {
        public class BoardIdAndThreadTitleKeywords
        {
            public string BoardId { get; }
            public string[] ThreadTitleKeywords { get; }
            public BoardIdAndThreadTitleKeywords(string boardId,string[] threadTitleKeywords)
            {
                BoardId = boardId;
                ThreadTitleKeywords = threadTitleKeywords;
            }
        }

        IEnumerable<ThreadMappingRuleEntry> rules;

        public ThreadMapping(IEnumerable<ThreadMappingRuleEntry> rules)
        {
            this.rules = rules;
        }

        /// <summary>
        /// 対応する板IDとスレッドタイトルを返す 対応がなければnull
        /// </summary>
        /// <returns>対応する板IDとスレッドタイトル スレッドタイトルはnullの可能性もある</returns>
        public BoardIdAndThreadTitleKeywords Get(ChannelEntry channel)
        {
            string boardId = null;
            string[] threadTitle =null;
            foreach(ThreadMappingRuleEntry rule in rules)
            {
                switch (rule.Target)
                {
                    case ThreadMappingRuleTarget.Flags:
                        if (((ChannelFlags)rule.Value & channel.Flags) != 0)
                        {
                            boardId = rule.BoardId;
                            threadTitle = rule.ThreadTitleKeywords;
                        }
                        break;
                    case ThreadMappingRuleTarget.NSId:
                        if (rule.Value == (channel.NetworkId << 16 | channel.ServiceId))
                        {
                            boardId = rule.BoardId;
                            threadTitle = rule.ThreadTitleKeywords;
                        }
                        break;
                    case ThreadMappingRuleTarget.NId:
                        if (rule.Value == channel.NetworkId)
                        {
                            boardId = rule.BoardId;
                            threadTitle = rule.ThreadTitleKeywords;
                        }
                        break;
                }
            }

            if (boardId == null)
                return null;
            return new BoardIdAndThreadTitleKeywords(boardId,threadTitle);
        }
    }
}
