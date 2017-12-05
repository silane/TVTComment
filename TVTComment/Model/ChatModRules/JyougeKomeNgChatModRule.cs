using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.ChatModRules
{
    class JyougeKomeNgChatModRule:IChatModRule
    {
        public string Description => "上下コメNG";
        public IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> TargetChatCollectServiceEntries { get; }

        public JyougeKomeNgChatModRule(IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> targetChatCollectServiceEntry)
        {
            TargetChatCollectServiceEntries = targetChatCollectServiceEntry;
        }

        public bool Modify(Chat chat)
        {
            if (chat.Position == Chat.PositionType.Normal)
                return false;
            chat.SetNg(true);
            return true;
        }
    }
}
