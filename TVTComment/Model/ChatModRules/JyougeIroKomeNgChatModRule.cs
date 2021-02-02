using System.Collections.Generic;

namespace TVTComment.Model.ChatModRules
{
    class JyougeIroKomeNgChatModRule : IChatModRule
    {
        public string Description => "上下コメかつ色コメNG";
        public IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> TargetChatCollectServiceEntries { get; }

        public JyougeIroKomeNgChatModRule(IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> targetChatCollectServiceEntry)
        {
            TargetChatCollectServiceEntries = targetChatCollectServiceEntry;
        }

        public bool Modify(Chat chat)
        {
            if (chat.Position == Chat.PositionType.Normal || chat.Color.ToArgb() == System.Drawing.Color.White.ToArgb())
                return false;
            chat.SetNg(true);
            return true;
        }
    }
}
