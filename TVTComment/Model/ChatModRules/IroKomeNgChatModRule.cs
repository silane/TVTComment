using System.Collections.Generic;

namespace TVTComment.Model.ChatModRules
{
    class IroKomeNgChatModRule : IChatModRule
    {
        public string Description => "色コメNG";
        public IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> TargetChatCollectServiceEntries { get; }

        public IroKomeNgChatModRule(IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> targetChatCollectServiceEntry)
        {
            TargetChatCollectServiceEntries = targetChatCollectServiceEntry;
        }

        public bool Modify(Chat chat)
        {
            if (chat.Color.ToArgb() == System.Drawing.Color.White.ToArgb())
                return false;
            chat.SetNg(true);
            return true;
        }
    }
}
