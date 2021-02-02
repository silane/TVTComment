using System.Collections.Generic;
using System.Drawing;

namespace TVTComment.Model.ChatModRules
{
    class SetColorChatModRule : IChatModRule
    {

        public string Description => $"色を{Color.Name}に変更";
        public IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> TargetChatCollectServiceEntries { get; }
        public Color Color { get; }

        public SetColorChatModRule(
            IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> targetServiceEntries,
            Color color
        )
        {
            TargetChatCollectServiceEntries = targetServiceEntries;
            Color = color;
        }

        public bool Modify(Chat chat)
        {
            chat.SetColor(Color);
            return true;
        }
    }
}
