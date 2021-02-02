using System.Collections.Generic;
using System.Linq;

namespace TVTComment.Model.ChatModRules
{
    class SmallOnMultiLineChatModRule : IChatModRule
    {
        public string Description => $"{LineCount}行以上のコメントをsmallにする";
        public IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> TargetChatCollectServiceEntries { get; }
        public int LineCount { get; }

        public SmallOnMultiLineChatModRule(IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> targetChatCollectServiceEntries, int lineCount)
        {
            TargetChatCollectServiceEntries = targetChatCollectServiceEntries;
            LineCount = lineCount;
        }

        public bool Modify(Chat chat)
        {
            if (chat.Text.Where(x => x == '\n').Count() < LineCount - 1)
                return false;
            chat.SetSize(Chat.SizeType.Small);
            return true;
        }
    }
}
